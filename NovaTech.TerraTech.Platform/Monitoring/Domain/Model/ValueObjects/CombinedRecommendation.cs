namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

public record CombinedRecommendation(
    bool SuspensionRecommended,
    int EstimatedWaterSavingsLiters,
    string Message
);
