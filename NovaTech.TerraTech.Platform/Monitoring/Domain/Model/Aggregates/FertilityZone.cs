
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;

public class FertilityZone
{
    private const double DefaultMoistureMin = 25.0;
    private const double DefaultMoistureMax = 75.0;

    private const double DefaultNitrogenMin = 20.0;
    private const double DefaultNitrogenMax = 40.0;

    private const double DefaultPhosphorusMin = 15.0;
    private const double DefaultPhosphorusMax = 30.0;

    private const double DefaultPotassiumMin = 30.0;
    private const double DefaultPotassiumMax = 50.0;

    protected FertilityZone()
    {
        Name = null!;
        Boundary = null!;
    }

    public FertilityZone(
        int fieldId,
        int deviceId,
        string name,
        IReadOnlyList<FieldVertex> boundary)
    {
        if (fieldId <= 0)
            throw new ArgumentException("FieldId must be valid.");

        if (deviceId <= 0)
            throw new ArgumentException("DeviceId must be valid.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zone name is required.");

        ArgumentNullException.ThrowIfNull(boundary);

        var validatedBoundary = FieldBoundary.Validate(boundary);

        if (validatedBoundary is null)
            throw new ArgumentException("Zone boundary is required.");

        FieldId = fieldId;
        DeviceId = deviceId;
        Name = name.Trim();
        Boundary = validatedBoundary;

        ResetThresholds();
    }

    public int Id { get; private set; }

    public int FieldId { get; private set; }

    public int DeviceId { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyList<FieldVertex>? Boundary { get; private set; }

    public double MoistureMin { get; private set; } = DefaultMoistureMin;

    public double MoistureMax { get; private set; } = DefaultMoistureMax;

    public double NitrogenMin { get; private set; } = DefaultNitrogenMin;

    public double NitrogenMax { get; private set; } = DefaultNitrogenMax;

    public double PhosphorusMin { get; private set; } = DefaultPhosphorusMin;

    public double PhosphorusMax { get; private set; } = DefaultPhosphorusMax;

    public double PotassiumMin { get; private set; } = DefaultPotassiumMin;

    public double PotassiumMax { get; private set; } = DefaultPotassiumMax;

    public void SetThresholds(
        double moistureMin,
        double moistureMax,
        double nitrogenMin,
        double nitrogenMax,
        double phosphorusMin,
        double phosphorusMax,
        double potassiumMin,
        double potassiumMax)
    {
        ValidateRange("Moisture", moistureMin, moistureMax, 0, 100);

        ValidateRange(
            "Nitrogen",
            nitrogenMin,
            nitrogenMax,
            0,
            double.MaxValue);

        ValidateRange(
            "Phosphorus",
            phosphorusMin,
            phosphorusMax,
            0,
            double.MaxValue);

        ValidateRange(
            "Potassium",
            potassiumMin,
            potassiumMax,
            0,
            double.MaxValue);

        MoistureMin = moistureMin;
        MoistureMax = moistureMax;

        NitrogenMin = nitrogenMin;
        NitrogenMax = nitrogenMax;

        PhosphorusMin = phosphorusMin;
        PhosphorusMax = phosphorusMax;

        PotassiumMin = potassiumMin;
        PotassiumMax = potassiumMax;
    }

    public void ResetThresholds()
    {
        MoistureMin = DefaultMoistureMin;
        MoistureMax = DefaultMoistureMax;

        NitrogenMin = DefaultNitrogenMin;
        NitrogenMax = DefaultNitrogenMax;

        PhosphorusMin = DefaultPhosphorusMin;
        PhosphorusMax = DefaultPhosphorusMax;

        PotassiumMin = DefaultPotassiumMin;
        PotassiumMax = DefaultPotassiumMax;
    }

    private static void ValidateRange(
        string nutrient,
        double minimum,
        double maximum,
        double allowedMinimum,
        double allowedMaximum)
    {
        if (
            double.IsNaN(minimum) ||
            double.IsNaN(maximum) ||
            double.IsInfinity(minimum) ||
            double.IsInfinity(maximum) ||
            minimum < allowedMinimum ||
            maximum > allowedMaximum ||
            minimum >= maximum)
        {
            throw new ArgumentException(
                $"Invalid {nutrient} threshold range.");
        }
    }
}
