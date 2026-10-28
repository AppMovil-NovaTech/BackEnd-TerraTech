namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

public record SatelliteBounds(
    double North,
    double South,
    double East,
    double West
);

public record SatelliteImageRecord(
    int Id,
    int FieldId,
    DateTime CapturedAtUtc,
    string ImageUrl,
    double NdviAverage,
    double CloudCoveragePercent,
    SatelliteBounds Bounds
);
