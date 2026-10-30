using System.Text.Json.Serialization;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;

public record OpenMeteoResponse(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("current")] OpenMeteoCurrent? Current,
    [property: JsonPropertyName("hourly")] OpenMeteoHourly? Hourly
);
