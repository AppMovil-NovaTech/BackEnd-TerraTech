using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Transform;

public static class ProductRatingSummaryResourceFromEntityAssembler
{
    public static ProductRatingSummaryResource ToResourceFromEntity(ProductRatingSummary entity)
    {
        var breakdown = new RatingBreakdownResource(
            entity.OneStar,
            entity.TwoStars,
            entity.ThreeStars,
            entity.FourStars,
            entity.FiveStars);

        return new ProductRatingSummaryResource(
            entity.ProductId,
            entity.AverageRating,
            entity.TotalReviews,
            breakdown);
    }
}
