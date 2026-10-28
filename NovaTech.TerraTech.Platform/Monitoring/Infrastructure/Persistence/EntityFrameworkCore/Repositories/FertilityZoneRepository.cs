using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class FertilityZoneRepository(AppDbContext context)
    : BaseRepository<FertilityZone>(context), IFertilityZoneRepository
{
    public async Task<IEnumerable<FertilityZone>> FindByFieldIdAsync(
        int fieldId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<FertilityZone>()
            .Where(zone => zone.FieldId == fieldId)
            .ToListAsync(cancellationToken);
    }
}
