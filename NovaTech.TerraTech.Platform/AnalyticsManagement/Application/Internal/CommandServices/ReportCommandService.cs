using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Errors;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Services;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Internal.CommandServices;
public class ReportCommandService(
    IReportRepository reportRepository,
    IFieldRepository fieldRepository,
    IDeviceRepository deviceRepository,
    ISensorDataRepository sensorDataRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReportCommandService> logger) : IReportCommandService
{
    public async Task<Report?> GenerateFieldProjectionAsync(int fieldId, CancellationToken cancellationToken = default)
    {
        var field = await fieldRepository.FindByIdAsync(fieldId, cancellationToken);
        if (field is null)
            return null;

        var inventories = (await inventoryRepository.FindByFieldIdAsync(fieldId, cancellationToken)).ToList();
        var historicalKg = inventories.Sum(i => i.TotalWeightKg);
        var seasonsCount = inventories.Count;
        var historicalAverageKg = seasonsCount > 0 ? historicalKg / seasonsCount : 0m;

        var cropName = !string.IsNullOrWhiteSpace(field.CropName)
            ? field.CropName!
            : inventories.LastOrDefault()?.Name ?? "Unspecified";

        var devices = (await deviceRepository.FindByFieldIdAsync(new FieldId(fieldId), cancellationToken)).ToList();
        var latestReadings = new List<SensorReading>();
        foreach (var device in devices)
        {
            var reading = await sensorDataRepository.Latest(device.Id, cancellationToken);
            if (reading is not null)
                latestReadings.Add(reading);
        }

        decimal environmentalFactor;
        string factorReason;
        double meanMoisture = 0;

        if (latestReadings.Count > 0)
        {
            meanMoisture = latestReadings.Average(r => r.MoisturePercent);
            var meanNitrogen = latestReadings.Average(r => r.NitrogenPpm);
            var meanTemp = latestReadings.Average(r => r.SoilTemperatureC);

            if (meanMoisture < 20 || meanMoisture > 85 || meanNitrogen < 10 || meanTemp < 5 || meanTemp > 40)
            {
                environmentalFactor = 0.85m;
                factorReason = "Suboptimal moisture or nutrient levels (-15% expected)";
            }
            else if (meanMoisture is >= 30 and <= 75 && meanNitrogen >= 20)
            {
                environmentalFactor = 1.05m;
                factorReason = "Optimal moisture and nutrient levels (+5% expected)";
            }
            else
            {
                environmentalFactor = 1.00m;
                factorReason = "Standard moisture and nutrient levels (baseline expected)";
            }
        }
        else
        {
            environmentalFactor = 1.00m;
            factorReason = "Baseline environmental conditions (no active sensor readings)";
        }

        var projectedYieldKg = Math.Round(historicalAverageKg * environmentalFactor, 2);
        var confidenceScore = seasonsCount switch
        {
            0 => 0.00,
            1 => 0.85,
            2 => 0.90,
            _ => 0.95
        };

        var primaryDeviceId = devices.FirstOrDefault()?.Id ?? 0;
        var now = DateTimeOffset.UtcNow;

        var existingReport = await reportRepository.FindByFieldIdAsync(fieldId, cancellationToken);
        if (existingReport is not null)
        {
            existingReport.UpdateProjection(
                primaryDeviceId,
                cropName,
                historicalKg,
                projectedYieldKg,
                confidenceScore,
                environmentalFactor,
                factorReason,
                meanMoisture,
                now);
            reportRepository.Update(existingReport);
            await unitOfWork.CompleteAsync(cancellationToken);
            return existingReport;
        }

        var report = new Report(
            fieldId,
            primaryDeviceId,
            cropName,
            historicalKg,
            projectedYieldKg,
            confidenceScore,
            environmentalFactor,
            factorReason,
            meanMoisture,
            now);

        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return report;
    }
    public async Task<Result<Report>> Handle(CreateReportCommand command, CancellationToken cancellationToken = default)
    {
        var deviceId = command.DeviceId;
        var generatedAt = command.GeneratedAt;
        var existing = await reportRepository.FindByDeviceIdAndGeneratedAtAsync(deviceId, generatedAt, cancellationToken);
        if (existing != null)
        {
            logger.LogWarning("Duplicate report for DeviceId {DeviceId} on {GeneratedAt}", command.DeviceId, command.GeneratedAt);
            return Result<Report>.Failure(CreateReportError.DuplicateReport, "A report for this device on the same date already exists.");
        }

        try
        {
            var report = new Report(command);
            await reportRepository.AddAsync(report, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Report>.Success(report);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while creating report for DeviceId {DeviceId}", command.DeviceId);
            return Result<Report>.Failure(CreateReportError.UnexpectedError, ex.Message);
        }
        catch (DbUpdateException ex)when (IsDuplicateKeyViolation(ex))
        {
            logger.LogWarning(ex, "Duplicate key violation creating report for DeviceId {DeviceId}", command.DeviceId);
            return Result<Report>.Failure(CreateReportError.DuplicateReport, "Database duplicate key violation occurred.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database update failed creating report for DeviceId {DeviceId}", command.DeviceId);
            return Result<Report>.Failure(CreateReportError.UnexpectedError, "Database update failed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating report for DeviceId {DeviceId}", command.DeviceId);
            return Result<Report>.Failure(CreateReportError.UnexpectedError, ex.Message);
        }
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException exception)
    {
        return DatabaseErrors.IsUniqueViolation(exception);
    }

    public async Task<Result<Report>> Handle(UpdateReportCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await reportRepository.FindByIdAsync(command.Id, cancellationToken);
            if (report is null)
            {
                logger.LogWarning("Report with id {Id} not found for update", command.Id);
                return Result<Report>.Failure(UpdateReportError.ReportNotFound, $"Report with id {command.Id} not found.");
            }

            report.UpdateStatistics(command.MeanValue, command.Variance, command.StandardDeviation, command.TechnicalInterpretation);
            reportRepository.Update(report);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Report {Id} updated successfully", command.Id);
            return Result<Report>.Success(report);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while updating report {Id}", command.Id);
            return Result<Report>.Failure(UpdateReportError.InvalidData, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating report {Id}", command.Id);
            return Result<Report>.Failure(UpdateReportError.UnexpectedError, ex.Message);
        }
    }
}
