namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

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
