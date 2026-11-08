namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public record CombinedRecommendationResource(
    bool SuspensionRecommended,
    int EstimatedWaterSavingsLiters,
    string Message
);
