using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.Services;

public class WeatherForecastService(
    IFieldRepository fieldRepository,
    IDeviceRepository deviceRepository,
    ISensorDataRepository sensorDataRepository,
    IWeatherForecastClient weatherForecastClient,
    IMemoryCache memoryCache,
    ILogger<WeatherForecastService> logger) : IWeatherForecastService
{
    public async Task<FieldWeatherResult?> GetWeatherByFieldIdAsync(int fieldId, CancellationToken cancellationToken = default)
    {
        var field = await fieldRepository.FindByIdAsync(fieldId, cancellationToken);
        if (field is null)
            return null;

        var lat = field.LocationLatLong.Latitude;
        var lon = field.LocationLatLong.Longitude;
        var cacheKey = $"weather_forecast_{fieldId}";

        if (!memoryCache.TryGetValue(cacheKey, out WeatherForecastData? forecast) || forecast is null)
        {
            logger.LogInformation("Cache miss for weather forecast on field {FieldId}. Calling external provider.", fieldId);
            forecast = await weatherForecastClient.GetForecastAsync(lat, lon, cancellationToken);
            if (forecast is null)
                return null;

            memoryCache.Set(cacheKey, forecast, TimeSpan.FromHours(3));
        }

        double? latestMoisturePercent = null;
        var devices = await deviceRepository.FindByFieldIdAsync(new FieldId(fieldId), cancellationToken);
        foreach (var device in devices)
        {
            var reading = await sensorDataRepository.Latest(device.Id, cancellationToken);
            if (reading is not null)
            {
                latestMoisturePercent = reading.MoisturePercent;
                break;
            }
        }

        var suspensionRecommended = (forecast.PrecipitationProbability24h >= 80 || forecast.ExpectedPrecipitationMm >= 10)
                                   && (latestMoisturePercent is null || latestMoisturePercent <= 35);

        var recommendation = suspensionRecommended
            ? new CombinedRecommendation(
                SuspensionRecommended: true,
                EstimatedWaterSavingsLiters: 500,
                Message: "Se recomienda suspender el riego planificado para hoy debido a lluvias inminentes. Ahorro estimado: 500 L.")
            : new CombinedRecommendation(
                SuspensionRecommended: false,
                EstimatedWaterSavingsLiters: 0,
                Message: "Condiciones meteorológicas estables. Mantener el plan de riego habitual.");

        return new FieldWeatherResult(
            FieldId: field.Id,
            FieldName: field.Name.Value,
            ForecastTimestamp: forecast.ForecastTimestampUtc,
            CurrentTemperatureC: forecast.CurrentTemperatureC,
            RelativeHumidityPercent: forecast.RelativeHumidityPercent,
            WindSpeedKmh: forecast.WindSpeedKmh,
            PrecipitationProbability24h: forecast.PrecipitationProbability24h,
            ExpectedPrecipitationMm: forecast.ExpectedPrecipitationMm,
            CombinedRecommendation: recommendation
        );
    }
}
