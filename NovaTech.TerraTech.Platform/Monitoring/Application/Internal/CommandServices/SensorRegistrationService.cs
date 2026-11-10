using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;
public class SensorRegistrationService(ISensorDataRepository sensors, IFieldRepository fields, IDeviceRepository devices, IUnitOfWork unitOfWork)
{
    public async Task<Device> Register(string? code, string? mac, int fieldId, string name, string status = "OFFLINE", DateTimeOffset? lastSync = null, CancellationToken ct = default)
    {
        if (await fields.FindByIdAsync(fieldId, ct)is null)
            throw new ApiFailure(404, "FIELD_NOT_FOUND", "Field was not found.");
        var normalizedMac = mac?.Replace('-', ':').ToUpperInvariant();
        var sensor = await sensors.FindSensor(code, normalizedMac, ct);
        if (sensor is null)
            throw new ApiFailure(400, "UNKNOWN_SENSOR", "Sensor must exist in the provisioned catalog.");
        if (await sensors.IsAssociated(sensor.SensorCode, ct))
            throw new ApiFailure(409, "SENSOR_OCCUPIED", "Sensor is already associated.");
        var device = new Device(new CreateDeviceCommand(new FieldId(fieldId), new MacAddress(sensor.MacAddress), DeviceStatus.Create(status), lastSync ?? DateTimeOffset.UtcNow));
        device.Identify(sensor.SensorCode, name);
        await devices.AddAsync(device, ct);
        try
        {
            await unitOfWork.CompleteAsync(ct);
        }
        catch (DbUpdateException e)when (DatabaseErrors.IsUniqueViolation(e))
        {
            throw new ApiFailure(409, "SENSOR_OCCUPIED", "Sensor is already associated.");
        }

        return device;
    }
}
