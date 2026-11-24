using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;

public class FavoriteProductService(
    IFavoriteProductRepository favorites,
    IProductRepository products,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : IFavoriteProductService
{
    public async Task<bool> IsFavoriteAsync(int userId, int productId, CancellationToken ct = default)
    {
        ValidateIdentifiers(userId, productId);
        return await favorites.FindAsync(userId, productId, ct) is not null;
    }

    public async Task AddAsync(int userId, int productId, CancellationToken ct = default)
    {
        ValidateIdentifiers(userId, productId);
        if (await products.FindByIdAsync(productId, ct) is null)
            throw new KeyNotFoundException("Product not found.");
        // Notifications are addressed to profiles, so a subscriber must have one.
        if (!await favorites.UserHasProfileAsync(userId, ct))
            throw new InvalidOperationException("A profile is required to receive favorite product notifications.");
        if (await favorites.FindAsync(userId, productId, ct) is not null)
            return;
        await favorites.AddAsync(new FavoriteProduct(userId, productId), ct);
        await unitOfWork.CompleteAsync(ct);
    }

    public async Task RemoveAsync(int userId, int productId, CancellationToken ct = default)
    {
        ValidateIdentifiers(userId, productId);
        var favorite = await favorites.FindAsync(userId, productId, ct);
        if (favorite is null) return;
        favorites.Remove(favorite);
        await unitOfWork.CompleteAsync(ct);
    }

    public async Task QueuePriceChangeAsync(int productId, string productName, decimal previousPrice, decimal currentPrice, CancellationToken ct = default)
    {
        if (previousPrice == currentPrice) return;
        await QueueAsync(productId, "Cambio de precio de producto favorito",
            $"El producto '{productName}' cambió de precio de S/ {previousPrice:0.00} a S/ {currentPrice:0.00}.", ct);
    }

    public async Task QueueAvailabilityChangeAsync(int productId, string productName, int previousQuantity, int currentQuantity, CancellationToken ct = default)
    {
        if ((previousQuantity > 0) == (currentQuantity > 0)) return;
        var message = currentQuantity > 0
            ? $"El producto '{productName}' volvió a estar disponible. Unidades disponibles: {currentQuantity}."
            : $"El producto '{productName}' se agotó y actualmente no está disponible.";
        await QueueAsync(productId, "Disponibilidad de producto favorito", message, ct);
    }

    private async Task QueueAsync(int productId, string title, string message, CancellationToken ct)
    {
        var profileIds = await favorites.FindSubscriberProfileIdsAsync(productId, ct);
        foreach (var profileId in profileIds)
            await notifications.AddAsync(new Notification(new CreateNotificationCommand(profileId, title, message, true)), ct);
    }

    private static void ValidateIdentifiers(int userId, int productId)
    {
        if (userId <= 0 || productId <= 0)
            throw new ArgumentException("Valid user and product identifiers are required.");
    }
}
