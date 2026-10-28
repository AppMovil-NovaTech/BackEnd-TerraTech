using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using Xunit;

namespace TerraTech.IntegrationTests;

public class FertilityZoneThresholdTests
{
    private static readonly IReadOnlyList<FieldVertex> Boundary =
    [
        new FieldVertex(-12.0000, -77.0000),
        new FieldVertex(-12.0000, -77.0005),
        new FieldVertex(-12.0005, -77.0005),
        new FieldVertex(-12.0005, -77.0000)
    ];

    [Fact]
    public void DefaultThresholdsShouldMatchDomainDefaults()
    {
        var zone = new FertilityZone(1, 1, "Zona Norte", Boundary);

        Assert.Equal(25.0, zone.MoistureMin);
        Assert.Equal(75.0, zone.MoistureMax);
        Assert.Equal(20.0, zone.NitrogenMin);
        Assert.Equal(40.0, zone.NitrogenMax);
        Assert.Equal(15.0, zone.PhosphorusMin);
        Assert.Equal(30.0, zone.PhosphorusMax);
        Assert.Equal(30.0, zone.PotassiumMin);
        Assert.Equal(50.0, zone.PotassiumMax);
    }

    [Fact]
    public void SetThresholdsShouldUpdateAllValuesWhenValid()
    {
        var zone = new FertilityZone(1, 1, "Zona Norte", Boundary);

        zone.SetThresholds(30.0, 70.0, 25.0, 45.0, 20.0, 35.0, 35.0, 55.0);

        Assert.Equal(30.0, zone.MoistureMin);
        Assert.Equal(70.0, zone.MoistureMax);
        Assert.Equal(25.0, zone.NitrogenMin);
        Assert.Equal(45.0, zone.NitrogenMax);
        Assert.Equal(20.0, zone.PhosphorusMin);
        Assert.Equal(35.0, zone.PhosphorusMax);
        Assert.Equal(35.0, zone.PotassiumMin);
        Assert.Equal(55.0, zone.PotassiumMax);
    }

    [Fact]
    public void SetThresholdsShouldThrowWhenMoistureMinGreaterThanMax()
    {
        var zone = new FertilityZone(1, 1, "Zona Norte", Boundary);

        Assert.Throws<ArgumentException>(() =>
            zone.SetThresholds(80.0, 20.0, 20.0, 40.0, 15.0, 30.0, 30.0, 50.0));
    }

    [Fact]
    public void SetThresholdsShouldThrowWhenMoistureOutOfRange()
    {
        var zone = new FertilityZone(1, 1, "Zona Norte", Boundary);

        Assert.Throws<ArgumentException>(() =>
            zone.SetThresholds(-5.0, 50.0, 20.0, 40.0, 15.0, 30.0, 30.0, 50.0));

        Assert.Throws<ArgumentException>(() =>
            zone.SetThresholds(20.0, 105.0, 20.0, 40.0, 15.0, 30.0, 30.0, 50.0));
    }

    [Fact]
    public void ResetThresholdsShouldRestoreDefaultValues()
    {
        var zone = new FertilityZone(1, 1, "Zona Norte", Boundary);
        zone.SetThresholds(30.0, 60.0, 10.0, 20.0, 10.0, 20.0, 10.0, 20.0);

        zone.ResetThresholds();

        Assert.Equal(25.0, zone.MoistureMin);
        Assert.Equal(75.0, zone.MoistureMax);
        Assert.Equal(20.0, zone.NitrogenMin);
        Assert.Equal(40.0, zone.NitrogenMax);
        Assert.Equal(15.0, zone.PhosphorusMin);
        Assert.Equal(30.0, zone.PhosphorusMax);
        Assert.Equal(30.0, zone.PotassiumMin);
        Assert.Equal(50.0, zone.PotassiumMax);
    }
}
