
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;

public class SensorReadingCommandService(
    ISensorDataRepository readings,
    IDeviceRepository devices,
    IFieldRepository fields,
    IFertilityZoneRepository fertilityZones,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<SensorReading> Register(
        int deviceId,
        double moisture,
        double temperature,
        double nitrogen,
        double phosphorus,
        double potassium,
        CancellationToken ct)
    {
        if (!double.IsFinite(moisture) ||
            moisture < 0 ||
            moisture > 100 ||
            !double.IsFinite(temperature) ||
            !ValidNutrient(nitrogen) ||
            !ValidNutrient(phosphorus) ||
            !ValidNutrient(potassium))
        {
            throw new ApiFailure(
                400,
                "INVALID_READING",
                "The sensor reading contains invalid values."
            );
        }

        var device = await devices.FindByIdAsync(
            deviceId,
            ct
        ) ?? throw new ApiFailure(
            404,
            "DEVICE_NOT_FOUND",
            "Device was not found."
        );

        var fieldId = device.FieldId.Value;

        var field = await fields.FindByIdAsync(
            fieldId,
            ct
        ) ?? throw new ApiFailure(
            404,
            "FIELD_NOT_FOUND",
            "Field was not found."
        );

        var reading = new SensorReading
        {
            DeviceId = deviceId,
            RecordedAt = clock.GetUtcNow().UtcDateTime,
            MoisturePercent = moisture,
            SoilTemperatureC = temperature,
            NitrogenPpm = nitrogen,
            PhosphorusPpm = phosphorus,
            PotassiumPpm = potassium,
            Source = "SIMULATED"
        };

        await readings.AddReadingAsync(reading, ct);

        var zones = await fertilityZones.FindByFieldIdAsync(
            fieldId,
            ct
        );

        foreach (var zone in zones)
        {
            if (zone.DeviceId != deviceId)
                continue;

            var problems = EvaluateThresholds(
                zone,
                reading
            );

            if (problems.Count == 0)
                continue;

            var command = new CreateNotificationCommand(
                field.ProfileId.Value,
                $"Alerta de fertilidad: {zone.Name}",
                string.Join(" ", problems),
                true
            );

            var notification = new Notification(command);

            await notifications.AddAsync(
                notification,
                ct
            );
        }

        await unitOfWork.CompleteAsync(ct);

        return reading;
    }

    private static bool ValidNutrient(double value)
    {
        return double.IsFinite(value) && value >= 0;
    }

    private static List<string> EvaluateThresholds(
        FertilityZone zone,
        SensorReading reading)
    {
        var problems = new List<string>();

        Check(
            problems,
            "Humedad",
            reading.MoisturePercent,
            zone.MoistureMin,
            zone.MoistureMax,
            "%",
            "Revisar las condiciones de riego."
        );

        Check(
            problems,
            "Nitrogeno",
            reading.NitrogenPpm,
            zone.NitrogenMin,
            zone.NitrogenMax,
            "ppm",
            "Revisar el plan de fertilizacion."
        );

        Check(
            problems,
            "Fosforo",
            reading.PhosphorusPpm,
            zone.PhosphorusMin,
            zone.PhosphorusMax,
            "ppm",
            "Revisar los nutrientes del suelo."
        );

        Check(
            problems,
            "Potasio",
            reading.PotassiumPpm,
            zone.PotassiumMin,
            zone.PotassiumMax,
            "ppm",
            "Revisar los nutrientes del suelo."
        );

        return problems;
    }

    private static void Check(
        List<string> problems,
        string parameter,
        double value,
        double minimum,
        double maximum,
        string unit,
        string recommendation)
    {
        if (value < minimum)
        {
            problems.Add(
                $"{parameter}: {value:0.##} {unit} " +
                $"esta por debajo del minimo " +
                $"({minimum:0.##} {unit}). " +
                recommendation
            );
        }
        else if (value > maximum)
        {
            problems.Add(
                $"{parameter}: {value:0.##} {unit} " +
                $"supera el maximo " +
                $"({maximum:0.##} {unit}). " +
                recommendation
            );
        }
    }
}
