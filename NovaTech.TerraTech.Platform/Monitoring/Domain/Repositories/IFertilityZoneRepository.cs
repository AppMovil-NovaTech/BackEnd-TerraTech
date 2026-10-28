using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;

public interface IFertilityZoneRepository : IBaseRepository<FertilityZone>
{
    Task<IEnumerable<FertilityZone>> FindByFieldIdAsync(
        int fieldId,
        CancellationToken cancellationToken = default
    );
}
