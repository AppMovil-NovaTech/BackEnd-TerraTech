using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.QueryServices;

public class FertilityZoneQueryService(
    IFertilityZoneRepository fertilityZoneRepository
) : IFertilityZoneQueryService
{
    public async Task<IEnumerable<FertilityZone>> GetZonesByFieldIdAsync(
        int fieldId,
        CancellationToken cancellationToken = default)
    {
        return await fertilityZoneRepository.FindByFieldIdAsync(
            fieldId,
            cancellationToken
        );
    }
}
