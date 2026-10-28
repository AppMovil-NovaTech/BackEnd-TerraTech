using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Services;

public interface IWeatherForecastService
{
    Task<FieldWeatherResult?> GetWeatherByFieldIdAsync(int fieldId, CancellationToken cancellationToken = default);
}
