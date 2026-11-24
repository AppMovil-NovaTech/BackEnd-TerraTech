using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class FavoriteProductRepository(AppDbContext context)
    : BaseRepository<FavoriteProduct>(context), IFavoriteProductRepository
{
    public Task<FavoriteProduct?> FindAsync(int userId, int productId, CancellationToken ct = default) =>
        Context.Set<FavoriteProduct>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, ct);

    public Task<bool> UserHasProfileAsync(int userId, CancellationToken ct = default) =>
        Context.Set<Profile>().AnyAsync(p => p.UserId == userId, ct);

    public async Task<IReadOnlyList<int>> FindSubscriberProfileIdsAsync(int productId, CancellationToken ct = default)
    {
        // This is intentionally an internal, cross-user subscription lookup.
        // The controller never exposes other users' subscriptions.
        return await Context.Set<FavoriteProduct>()
            .IgnoreQueryFilters()
            .Where(f => f.ProductId == productId)
            .Join(Context.Set<Profile>().IgnoreQueryFilters(),
                favorite => favorite.UserId,
                profile => profile.UserId,
                (_, profile) => profile.Id)
            .Distinct()
            .ToListAsync(ct);
    }
}
