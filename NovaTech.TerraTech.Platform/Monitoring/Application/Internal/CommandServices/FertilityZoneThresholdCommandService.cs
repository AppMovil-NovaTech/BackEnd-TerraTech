
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;

public class FertilityZoneThresholdCommandService(
    IFertilityZoneRepository fertilityZoneRepository,
    IUnitOfWork unitOfWork
) : IFertilityZoneThresholdCommandService
{
    public async Task<FertilityZone> UpdateThresholdsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var zone = await GetZoneAsync(
            fieldId,
            zoneId,
            cancellationToken
        );

        zone.SetThresholds(
            moistureMin,
            moistureMax,
            nitrogenMin,
            nitrogenMax,
            phosphorusMin,
            phosphorusMax,
            potassiumMin,
            potassiumMax
        );

        fertilityZoneRepository.Update(zone);

        await unitOfWork.CompleteAsync(cancellationToken);

        return zone;
    }

    public async Task<FertilityZone> ResetThresholdsAsync(
        int fieldId,
        int zoneId,
        CancellationToken cancellationToken = default)
    {
        var zone = await GetZoneAsync(
            fieldId,
            zoneId,
            cancellationToken
        );

        zone.ResetThresholds();

        fertilityZoneRepository.Update(zone);

        await unitOfWork.CompleteAsync(cancellationToken);

        return zone;
    }

    private async Task<FertilityZone> GetZoneAsync(
        int fieldId,
        int zoneId,
        CancellationToken cancellationToken)
    {
        var zone = await fertilityZoneRepository.FindByIdAsync(
            zoneId,
            cancellationToken
        );

        if (zone is null || zone.FieldId != fieldId)
            throw new KeyNotFoundException(
                "Fertility zone was not found in the selected field."
            );

        return zone;
    }
}
