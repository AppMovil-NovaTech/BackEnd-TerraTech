namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public record CombinedRecommendationResource(
    bool SuspensionRecommended,
    int EstimatedWaterSavingsLiters,
    string Message
);

public record FieldWeatherResource(
    int FieldId,
    string FieldName,
    DateTime ForecastTimestamp,
    double CurrentTemperatureC,
    double RelativeHumidityPercent,
    double WindSpeedKmh,
    double PrecipitationProbability24h,
    double ExpectedPrecipitationMm,
    CombinedRecommendationResource CombinedRecommendation
);
