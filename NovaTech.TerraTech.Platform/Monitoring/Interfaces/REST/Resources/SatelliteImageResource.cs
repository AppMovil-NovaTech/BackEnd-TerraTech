namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public record SatelliteBoundsResource(
    double North,
    double South,
    double East,
    double West
);

public record SatelliteImageResource(
    int FieldId,
    DateTime CapturedAt,
    string ImageUrl,
    double NdviAverage,
    double CloudCoveragePercent,
    SatelliteBoundsResource Bounds
);
