using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.QueryServices;

public class FertilityZoneThresholdQueryService(
    IFertilityZoneRepository fertilityZoneRepository
) : IFertilityZoneThresholdQueryService
{
    public async Task<FertilityZone> GetThresholdsAsync(
        int fieldId,
        int zoneId,
        CancellationToken cancellationToken = default)
    {
        var zone = await fertilityZoneRepository.FindByIdAsync(
            zoneId,
            cancellationToken
        );

        if (zone is null || zone.FieldId != fieldId)
            throw new KeyNotFoundException(
                "Fertility zone was not found in the selected field."
            );

        return zone;
    }
}
