using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Domain.Repositories;

public interface ICommentRepository : IBaseRepository<Comment>
{
    Task<IEnumerable<Comment>> FindByTargetProfileIdAsync(int targetProfileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> FindByProductIdAsync(int productId, int? rating = null, CancellationToken cancellationToken = default);
    Task<ProductRatingSummary> GetRatingSummaryByProductIdAsync(int productId, CancellationToken cancellationToken = default);
}
