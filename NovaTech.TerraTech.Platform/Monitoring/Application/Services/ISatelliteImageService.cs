using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Services;

public interface ISatelliteImageService
{
    Task<SatelliteImageRecord?> GetLatestImageAsync(int fieldId, CancellationToken cancellationToken = default);
    Task<SatelliteImageRecord?> GetImageByTargetDateAsync(int fieldId, DateTime targetDate, CancellationToken cancellationToken = default);
}
