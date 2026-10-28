using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Services;

public interface IFertilityZoneThresholdQueryService
{
    Task<FertilityZone> GetThresholdsAsync(
        int fieldId,
        int zoneId,
        CancellationToken cancellationToken = default
    );
}
