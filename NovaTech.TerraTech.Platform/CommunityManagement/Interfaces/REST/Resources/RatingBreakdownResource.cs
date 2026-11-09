using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;

[SwaggerSchema(Description = "Rating breakdown by stars")]
public record RatingBreakdownResource(
    [SwaggerParameter(Description = "Count of 1-star ratings")] int OneStar,
    [SwaggerParameter(Description = "Count of 2-star ratings")] int TwoStars,
    [SwaggerParameter(Description = "Count of 3-star ratings")] int ThreeStars,
    [SwaggerParameter(Description = "Count of 4-star ratings")] int FourStars,
    [SwaggerParameter(Description = "Count of 5-star ratings")] int FiveStars);
