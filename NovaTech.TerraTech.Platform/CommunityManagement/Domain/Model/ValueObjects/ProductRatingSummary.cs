namespace NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.ValueObjects;

public record ProductRatingSummary(
    int ProductId,
    double AverageRating,
    int TotalReviews,
    int OneStar,
    int TwoStars,
    int ThreeStars,
    int FourStars,
    int FiveStars);
