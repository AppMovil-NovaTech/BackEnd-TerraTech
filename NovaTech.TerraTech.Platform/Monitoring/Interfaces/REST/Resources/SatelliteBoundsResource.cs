namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public record SatelliteBoundsResource(
    double North,
    double South,
    double East,
    double West
);
