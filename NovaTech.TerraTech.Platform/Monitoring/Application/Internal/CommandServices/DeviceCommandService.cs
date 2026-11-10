using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Monitoring.Application.Errors;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;
public class DeviceCommandService(IDeviceRepository deviceRepository, IUnitOfWork unitOfWork, ILogger<DeviceCommandService> logger, SensorRegistrationService registration) : IDeviceCommandService
{
    public async Task<Result<Device>> Handle(CreateDeviceCommand command, CancellationToken cancellationToken = default)
    {
        var device = await registration.Register(null, command.MacAddress.Value, command.FieldId.Value, "Sensor", command.Status.Value, command.LastSync, cancellationToken);
        return Result<Device>.Success(device);
    }

    public async Task<Result<Device>> Handle(UpdateDeviceCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await deviceRepository.FindByIdAsync(command.Id, cancellationToken);
            if (device is null)
            {
                logger.LogWarning("Device with id {Id} not found for update", command.Id);
                return Result<Device>.Failure(CreateDeviceError.DeviceNotFound, $"Device with id {command.Id} not found.");
            }

            var existingDevice = await deviceRepository.FindByMacAddressAsync(command.MacAddress, cancellationToken);
            if (existingDevice is not null && existingDevice.Id != command.Id)
            {
                logger.LogWarning("MAC {MacAddress} already used by another device", command.MacAddress);
                return Result<Device>.Failure(CreateDeviceError.DuplicateDevice, $"MAC address {command.MacAddress} is already in use.");
            }

            if (device.MacAddress.Value.Replace('-', ':').ToUpperInvariant() != command.MacAddress.Value.Replace('-', ':').ToUpperInvariant())
                return Result<Device>.Failure(CreateDeviceError.InvalidData, "Registered MAC cannot be changed.");
            device.Update(command);
            deviceRepository.Update(device);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Device {Id} updated successfully", command.Id);
            return Result<Device>.Success(device);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while updating device {Id}", command.Id);
            return Result<Device>.Failure(CreateDeviceError.InvalidData, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating device {Id}", command.Id);
            return Result<Device>.Failure(CreateDeviceError.UnexpectedError, ex.Message);
        }
    }

    public async Task<Result<Device>> Handle(DeleteDeviceCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await deviceRepository.FindByIdAsync(command.Id, cancellationToken);
            if (device is null)
            {
                logger.LogWarning("Device with id {Id} not found for deletion", command.Id);
                return Result<Device>.Failure(CreateDeviceError.DeviceNotFound, $"Device with id {command.Id} not found.");
            }

            deviceRepository.Remove(device);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Device {Id} deleted successfully", command.Id);
            return Result<Device>.Success(device);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting device {Id}", command.Id);
            return Result<Device>.Failure(CreateDeviceError.UnexpectedError, ex.Message);
        }
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException exception)
    {
        return DatabaseErrors.IsUniqueViolation(exception);
    }
}
