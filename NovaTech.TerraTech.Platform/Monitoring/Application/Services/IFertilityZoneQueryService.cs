using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Services;

public interface IFertilityZoneQueryService
{
    Task<IEnumerable<FertilityZone>> GetZonesByFieldIdAsync(
        int fieldId,
        CancellationToken cancellationToken = default
    );
}
