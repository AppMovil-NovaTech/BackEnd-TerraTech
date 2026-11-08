using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class CommentRepository(AppDbContext context) : BaseRepository<Comment>(context), ICommentRepository
{
    public async Task<IEnumerable<Comment>> FindByTargetProfileIdAsync(int targetProfileId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Comment>().Where(c => c.TargetProfileId == targetProfileId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Comment>> FindByProductIdAsync(int productId, int? rating = null, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Comment>().Where(c => c.ProductId == productId);
        if (rating.HasValue)
        {
            query = query.Where(c => c.Rating == rating.Value);
        }
        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<ProductRatingSummary> GetRatingSummaryByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var ratings = await Context.Set<Comment>()
            .Where(c => c.ProductId == productId)
            .Select(c => c.Rating)
            .ToListAsync(cancellationToken);

        if (ratings.Count == 0)
        {
            return new ProductRatingSummary(productId, 0.0, 0, 0, 0, 0, 0, 0);
        }

        var averageRating = Math.Round(ratings.Average(), 1);
        var totalReviews = ratings.Count;
        var oneStar = ratings.Count(r => r == 1);
        var twoStars = ratings.Count(r => r == 2);
        var threeStars = ratings.Count(r => r == 3);
        var fourStars = ratings.Count(r => r == 4);
        var fiveStars = ratings.Count(r => r == 5);

        return new ProductRatingSummary(
            productId,
            averageRating,
            totalReviews,
            oneStar,
            twoStars,
            threeStars,
            fourStars,
            fiveStars);
    }
}
