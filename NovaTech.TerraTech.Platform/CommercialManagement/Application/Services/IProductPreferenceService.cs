namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;

public interface IProductPreferenceService
{
    Task<IReadOnlyList<string>> GetMineAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAvailableCategoriesAsync(CancellationToken ct = default);
    Task AddAsync(int userId, string category, CancellationToken ct = default);
    Task RemoveAsync(int userId, string category, CancellationToken ct = default);
    // Queue US31 notifications in the current UnitOfWork (no nested SaveChanges).
    Task QueueNewProductAsync(string productName, string category, int? publisherUserId, CancellationToken ct = default);
}
