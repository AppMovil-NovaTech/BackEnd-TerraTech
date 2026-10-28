using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Services;

public interface IFertilityZoneThresholdCommandService
{
    Task<FertilityZone> UpdateThresholdsAsync(
        int fieldId,
        int zoneId,
        double moistureMin,
        double moistureMax,
        double nitrogenMin,
        double nitrogenMax,
        double phosphorusMin,
        double phosphorusMax,
        double potassiumMin,
        double potassiumMax,
        CancellationToken cancellationToken = default
    );

    Task<FertilityZone> ResetThresholdsAsync(
        int fieldId,
        int zoneId,
        CancellationToken cancellationToken = default
    );
}
