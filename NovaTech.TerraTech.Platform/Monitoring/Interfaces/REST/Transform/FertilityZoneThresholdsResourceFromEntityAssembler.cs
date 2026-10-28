using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;

public static class FertilityZoneThresholdsResourceFromEntityAssembler
{
    public static FertilityZoneThresholdsResource ToResourceFromEntity(
        FertilityZone zone)
    {
        return new FertilityZoneThresholdsResource(
            zone.Id,
            zone.MoistureMin,
            zone.MoistureMax,
            zone.NitrogenMin,
            zone.NitrogenMax,
            zone.PhosphorusMin,
            zone.PhosphorusMax,
            zone.PotassiumMin,
            zone.PotassiumMax
        );
    }
}
