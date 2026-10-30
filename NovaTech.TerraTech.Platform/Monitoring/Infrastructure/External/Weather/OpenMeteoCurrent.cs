using System.Text.Json.Serialization;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;

public record OpenMeteoCurrent(
    [property: JsonPropertyName("time")] string? Time,
    [property: JsonPropertyName("temperature_2m")] double Temperature2m,
    [property: JsonPropertyName("relative_humidity_2m")] double RelativeHumidity2m,
    [property: JsonPropertyName("wind_speed_10m")] double WindSpeed10m
);
