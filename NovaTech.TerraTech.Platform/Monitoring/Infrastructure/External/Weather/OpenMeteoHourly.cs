using System.Text.Json.Serialization;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;

public record OpenMeteoHourly(
    [property: JsonPropertyName("time")] IReadOnlyList<string>? Time,
    [property: JsonPropertyName("precipitation_probability")] IReadOnlyList<double>? PrecipitationProbability,
    [property: JsonPropertyName("precipitation")] IReadOnlyList<double>? Precipitation
);
