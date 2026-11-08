namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

public record SatelliteImageResource(
    int FieldId,
    DateTime CapturedAt,
    string ImageUrl,
    double NdviAverage,
    double CloudCoveragePercent,
    SatelliteBoundsResource Bounds
);
