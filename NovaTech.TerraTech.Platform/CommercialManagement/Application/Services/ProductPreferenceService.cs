using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;

public sealed class ProductPreferenceService(
    IProductPreferenceRepository preferences,
    IProductRepository products,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : IProductPreferenceService
{
    public async Task<IReadOnlyList<string>> GetMineAsync(int userId, CancellationToken ct = default)
    {
        RequireUser(userId);
        return (await preferences.FindForUserAsync(userId, ct)).Select(p => p.Category).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetAvailableCategoriesAsync(CancellationToken ct = default)
    {
        var existing = await products.ListAsync(ct);
        return existing.Where(p => !string.IsNullOrWhiteSpace(p.Type))
            .Select(p => p.Type.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task AddAsync(int userId, string category, CancellationToken ct = default)
    {
        RequireUser(userId);
        var normalized = ProductPreference.NormalizeCategory(category);
        if (!await preferences.HasProfileAsync(userId, ct))
            throw new InvalidOperationException("A profile is required to subscribe to new product alerts.");
        if (await preferences.FindAsync(userId, normalized, ct) is not null) return;
        await preferences.AddAsync(new ProductPreference(userId, normalized), ct);
        await unitOfWork.CompleteAsync(ct);
    }

    public async Task RemoveAsync(int userId, string category, CancellationToken ct = default)
    {
        RequireUser(userId);
        var normalized = ProductPreference.NormalizeCategory(category);
        var preference = await preferences.FindAsync(userId, normalized, ct);
        if (preference is null) return;
        preferences.Remove(preference);
        await unitOfWork.CompleteAsync(ct);
    }

    public async Task QueueNewProductAsync(string productName, string category, int? publisherUserId, CancellationToken ct = default)
    {
        var normalized = ProductPreference.NormalizeCategory(category);
        var profileIds = await preferences.FindMatchingProfileIdsAsync(normalized, publisherUserId, ct);
        foreach (var profileId in profileIds)
        {
            var message = $"Se publicó '{productName}' en la categoría '{category.Trim()}', que coincide con tus preferencias.";
            await notifications.AddAsync(new Notification(new CreateNotificationCommand(
                profileId, "Nuevo producto según tus preferencias", message, true)), ct);
        }
    }

    private static void RequireUser(int userId)
    {
        if (userId <= 0) throw new ArgumentException("Authenticated user required.", nameof(userId));
    }
}
