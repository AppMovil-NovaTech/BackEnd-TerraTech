using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public sealed class ProductPreferenceRepository(AppDbContext db) : IProductPreferenceRepository
{
    public async Task<IReadOnlyList<ProductPreference>> FindForUserAsync(int userId, CancellationToken ct = default) =>
        await db.Set<ProductPreference>().Where(p => p.UserId == userId).OrderBy(p => p.Category).ToListAsync(ct);

    public Task<ProductPreference?> FindAsync(int userId, string category, CancellationToken ct = default) =>
        db.Set<ProductPreference>().FirstOrDefaultAsync(p => p.UserId == userId && p.Category == category, ct);

    public Task<bool> HasProfileAsync(int userId, CancellationToken ct = default) =>
        db.Set<Profile>().AnyAsync(p => p.UserId == userId, ct);

    public async Task<IReadOnlyList<int>> FindMatchingProfileIdsAsync(string category, int? excludedUserId, CancellationToken ct = default)
    {
        // This query is exclusively internal to fan-out notification delivery;
        // user-facing reads stay protected by EF ownership filters.
        return await db.Set<ProductPreference>().IgnoreQueryFilters()
            .Where(p => p.Category == category && (!excludedUserId.HasValue || p.UserId != excludedUserId.Value))
            .Join(db.Set<Profile>().IgnoreQueryFilters(), p => p.UserId, profile => profile.UserId,
                (_, profile) => profile.Id)
            .Distinct().ToListAsync(ct);
    }

    public Task AddAsync(ProductPreference preference, CancellationToken ct = default) =>
        db.Set<ProductPreference>().AddAsync(preference, ct).AsTask();

    public void Remove(ProductPreference preference) => db.Set<ProductPreference>().Remove(preference);
}
