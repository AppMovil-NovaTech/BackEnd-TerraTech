using System.Text.Json.Serialization;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;

public record OpenMeteoResponse(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("current")] OpenMeteoCurrent? Current,
    [property: JsonPropertyName("hourly")] OpenMeteoHourly? Hourly
);

public record OpenMeteoCurrent(
    [property: JsonPropertyName("time")] string? Time,
    [property: JsonPropertyName("temperature_2m")] double Temperature2m,
    [property: JsonPropertyName("relative_humidity_2m")] double RelativeHumidity2m,
    [property: JsonPropertyName("wind_speed_10m")] double WindSpeed10m
);

public record OpenMeteoHourly(
    [property: JsonPropertyName("time")] IReadOnlyList<string>? Time,
    [property: JsonPropertyName("precipitation_probability")] IReadOnlyList<double>? PrecipitationProbability,
    [property: JsonPropertyName("precipitation")] IReadOnlyList<double>? Precipitation
);
