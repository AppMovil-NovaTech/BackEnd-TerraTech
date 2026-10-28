using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;

public class FertilityZone
{
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
    }

    public int Id { get; private set; }

    public int FieldId { get; private set; }

    public int DeviceId { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyList<FieldVertex>? Boundary { get; private set; }
}
