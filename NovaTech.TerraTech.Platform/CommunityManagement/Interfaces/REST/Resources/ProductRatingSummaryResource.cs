using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;

[SwaggerSchema(Description = "Product rating summary resource")]
public record ProductRatingSummaryResource(
    [SwaggerParameter(Description = "Product identifier")] int ProductId,
    [SwaggerParameter(Description = "Average rating score")] double AverageRating,
    [SwaggerParameter(Description = "Total number of reviews")] int TotalReviews,
    [SwaggerParameter(Description = "Breakdown of star ratings")] RatingBreakdownResource RatingBreakdown);
