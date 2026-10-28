using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.External.Weather;

public interface IWeatherForecastClient
{
    Task<WeatherForecastData?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
