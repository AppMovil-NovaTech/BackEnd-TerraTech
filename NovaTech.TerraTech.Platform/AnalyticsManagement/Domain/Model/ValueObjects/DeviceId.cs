namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.ValueObjects;
public sealed record DeviceId
{
    public DeviceId(int value)
    {
        if (value < 0)
            throw new ArgumentException("DeviceId cannot be negative.", nameof(value));
        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString();
}
