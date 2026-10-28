namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public record FertilityZoneThresholdsResource(
    int ZoneId,
    double MoistureMin,
    double MoistureMax,
    double NitrogenMin,
    double NitrogenMax,
    double PhosphorusMin,
    double PhosphorusMax,
    double PotassiumMin,
    double PotassiumMax
);
