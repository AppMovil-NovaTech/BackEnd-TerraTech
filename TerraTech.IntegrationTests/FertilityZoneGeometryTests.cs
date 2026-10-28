
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using Xunit;

namespace TerraTech.IntegrationTests;

public class FertilityZoneGeometryTests
{
    private static readonly IReadOnlyList<FieldVertex> Field =
    [
        new FieldVertex(0, 0),
        new FieldVertex(0, 5),
        new FieldVertex(5, 5),
        new FieldVertex(5, 0)
    ];

    [Fact]
    public void ZoneInsideFieldShouldBeAccepted()
    {
        IReadOnlyList<FieldVertex> zone =
        [
            new FieldVertex(1, 1),
            new FieldVertex(1, 2),
            new FieldVertex(2, 2),
            new FieldVertex(2, 1)
        ];

        var result = FertilityZoneGeometry.IsWithinField(zone, Field);

        Assert.True(result);
    }

    [Fact]
    public void ZoneOutsideFieldShouldBeRejected()
    {
        IReadOnlyList<FieldVertex> zone =
        [
            new FieldVertex(6, 6),
            new FieldVertex(6, 7),
            new FieldVertex(7, 6)
        ];

        var result = FertilityZoneGeometry.IsWithinField(zone, Field);

        Assert.False(result);
    }

    [Fact]
    public void ZoneCrossingFieldBoundaryShouldBeRejected()
    {
        IReadOnlyList<FieldVertex> zone =
        [
            new FieldVertex(4, 4),
            new FieldVertex(4, 6),
            new FieldVertex(6, 4)
        ];

        var result = FertilityZoneGeometry.IsWithinField(zone, Field);

        Assert.False(result);
    }

    [Fact]
    public void ZoneTouchingFieldBoundaryShouldBeAccepted()
    {
        IReadOnlyList<FieldVertex> zone =
        [
            new FieldVertex(0, 0),
            new FieldVertex(0, 2),
            new FieldVertex(2, 2),
            new FieldVertex(2, 0)
        ];

        var result = FertilityZoneGeometry.IsWithinField(zone, Field);

        Assert.True(result);
    }

    [Fact]
    public void ZoneCrossingConcaveFieldExteriorShouldBeRejected()
    {
        IReadOnlyList<FieldVertex> concaveField =
        [
            new FieldVertex(0, 0),
            new FieldVertex(0, 6),
            new FieldVertex(2, 6),
            new FieldVertex(2, 2),
            new FieldVertex(6, 2),
            new FieldVertex(6, 0)
        ];

        IReadOnlyList<FieldVertex> zone =
        [
            new FieldVertex(1, 1),
            new FieldVertex(1, 5),
            new FieldVertex(5, 1)
        ];

        var result = FertilityZoneGeometry.IsWithinField(
            zone,
            concaveField
        );

        Assert.False(result);
    }

    [Fact]
    public void FieldWithoutBoundaryShouldRejectZone()
    {
        IReadOnlyList<FieldVertex> zone =
        [
            new FieldVertex(1, 1),
            new FieldVertex(1, 2),
            new FieldVertex(2, 1)
        ];

        var result = FertilityZoneGeometry.IsWithinField(zone, null);

        Assert.False(result);
    }
}
