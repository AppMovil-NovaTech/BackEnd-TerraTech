using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;

public interface IFavoriteProductRepository : IBaseRepository<FavoriteProduct>
{
    Task<FavoriteProduct?> FindAsync(int userId, int productId, CancellationToken ct = default);
    Task<bool> UserHasProfileAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> FindSubscriberProfileIdsAsync(int productId, CancellationToken ct = default);
}
