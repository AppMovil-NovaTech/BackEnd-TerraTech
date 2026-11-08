namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

public record FieldWeatherResult(
    int FieldId,
    string FieldName,
    DateTime ForecastTimestamp,
    double CurrentTemperatureC,
    double RelativeHumidityPercent,
    double WindSpeedKmh,
    double PrecipitationProbability24h,
    double ExpectedPrecipitationMm,
    CombinedRecommendation CombinedRecommendation
);
