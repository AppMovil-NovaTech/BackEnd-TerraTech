namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

public record WeatherForecastData(
    DateTime ForecastTimestampUtc,
    double CurrentTemperatureC,
    double RelativeHumidityPercent,
    double WindSpeedKmh,
    double PrecipitationProbability24h,
    double ExpectedPrecipitationMm
);
