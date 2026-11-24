namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;

public interface IFavoriteProductService
{
    Task<bool> IsFavoriteAsync(int userId, int productId, CancellationToken ct = default);
    Task AddAsync(int userId, int productId, CancellationToken ct = default);
    Task RemoveAsync(int userId, int productId, CancellationToken ct = default);
    // Queue notifications in the caller's UnitOfWork; do NOT save independently.
    Task QueuePriceChangeAsync(int productId, string productName, decimal previousPrice, decimal currentPrice, CancellationToken ct = default);
    Task QueueAvailabilityChangeAsync(int productId, string productName, int previousQuantity, int currentQuantity, CancellationToken ct = default);
}
