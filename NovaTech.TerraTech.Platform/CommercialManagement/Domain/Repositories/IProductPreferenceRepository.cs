using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;

public interface IProductPreferenceRepository
{
    Task<IReadOnlyList<ProductPreference>> FindForUserAsync(int userId, CancellationToken ct = default);
    Task<ProductPreference?> FindAsync(int userId, string category, CancellationToken ct = default);
    Task<bool> HasProfileAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> FindMatchingProfileIdsAsync(string category, int? excludedUserId, CancellationToken ct = default);
    Task AddAsync(ProductPreference preference, CancellationToken ct = default);
    void Remove(ProductPreference preference);
}
