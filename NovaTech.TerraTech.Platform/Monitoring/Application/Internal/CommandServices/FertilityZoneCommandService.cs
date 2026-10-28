
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;

public class FertilityZoneCommandService(
    IFieldQueryService fieldQueryService,
    IDeviceQueryService deviceQueryService,
    IFertilityZoneRepository fertilityZoneRepository,
    IUnitOfWork unitOfWork
) : IFertilityZoneCommandService
{
    public async Task<FertilityZone> CreateZoneAsync(
        int fieldId,
        int deviceId,
        string name,
        IReadOnlyList<FieldVertex> boundary,
        CancellationToken cancellationToken = default)
    {
        if (fieldId <= 0)
            throw new ArgumentException("Invalid field identifier.");

        if (deviceId <= 0)
            throw new ArgumentException("Invalid device identifier.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zone name is required.");

        if (name.Trim().Length > 100)
            throw new ArgumentException(
                "Zone name cannot exceed 100 characters."
            );

        ArgumentNullException.ThrowIfNull(boundary);

        var field = await fieldQueryService.GetFieldByIdAsync(
            fieldId,
            cancellationToken
        );

        if (field is null)
            throw new KeyNotFoundException("Field not found.");

        var device = await deviceQueryService.GetDeviceByIdAsync(
            deviceId,
            cancellationToken
        );

        if (device is null)
            throw new KeyNotFoundException("Device not found.");

        if (device.FieldId.Value != fieldId)
            throw new ArgumentException(
                "The sensor does not belong to the selected field."
            );

        if (field.Boundary is null)
            throw new ArgumentException(
                "The field must have a geographic boundary."
            );

        var zone = new FertilityZone(
            fieldId,
            deviceId,
            name,
            boundary
        );

        if (!FertilityZoneGeometry.IsWithinField(
                zone.Boundary!,
                field.Boundary))
        {
            throw new ArgumentException(
                "The fertility zone must be inside the field boundary."
            );
        }

        await fertilityZoneRepository.AddAsync(
            zone,
            cancellationToken
        );

        await unitOfWork.CompleteAsync(cancellationToken);

        return zone;
    }
}
