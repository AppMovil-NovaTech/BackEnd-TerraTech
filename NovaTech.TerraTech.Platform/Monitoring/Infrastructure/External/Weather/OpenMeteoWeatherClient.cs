using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;

public class OpenMeteoWeatherClient(HttpClient httpClient, ILogger<OpenMeteoWeatherClient> logger) : IWeatherForecastClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<WeatherForecastData?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            var latStr = latitude.ToString("F4", CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString("F4", CultureInfo.InvariantCulture);
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,wind_speed_10m&hourly=precipitation_probability,precipitation&forecast_days=2";

            var response = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, JsonOptions, cancellationToken);
            if (response?.Current is null)
                return null;

            var maxProbability24h = response.Hourly?.PrecipitationProbability?
                .Take(24)
                .DefaultIfEmpty(0)
                .Max() ?? 0;

            var totalPrecipitationNext6h = response.Hourly?.Precipitation?
                .Take(6)
                .DefaultIfEmpty(0)
                .Sum() ?? 0;

            return new WeatherForecastData(
                ForecastTimestampUtc: DateTime.UtcNow,
                CurrentTemperatureC: response.Current.Temperature2m,
                RelativeHumidityPercent: response.Current.RelativeHumidity2m,
                WindSpeedKmh: response.Current.WindSpeed10m,
                PrecipitationProbability24h: maxProbability24h,
                ExpectedPrecipitationMm: totalPrecipitationNext6h
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP failure while querying Open-Meteo for ({Latitude}, {Longitude})", latitude, longitude);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout querying Open-Meteo for ({Latitude}, {Longitude})", latitude, longitude);
            throw;
        }
    }
}
