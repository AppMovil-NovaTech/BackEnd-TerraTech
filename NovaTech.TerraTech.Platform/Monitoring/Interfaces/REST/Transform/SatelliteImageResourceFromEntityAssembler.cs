using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;

public static class SatelliteImageResourceFromEntityAssembler
{
    public static SatelliteImageResource ToResourceFromEntity(SatelliteImageRecord entity)
    {
        return new SatelliteImageResource(
            FieldId: entity.FieldId,
            CapturedAt: entity.CapturedAtUtc,
            ImageUrl: entity.ImageUrl,
            NdviAverage: entity.NdviAverage,
            CloudCoveragePercent: entity.CloudCoveragePercent,
            Bounds: new SatelliteBoundsResource(
                North: entity.Bounds.North,
                South: entity.Bounds.South,
                East: entity.Bounds.East,
                West: entity.Bounds.West
            )
        );
    }
}
