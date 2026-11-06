namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.ValueObjects;
public sealed record GeneratedAt
{
    public GeneratedAt(DateTimeOffset value)
    {
        if (value > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new ArgumentException("GeneratedAt cannot be in the far future.", nameof(value));
        if (value < new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))
            throw new ArgumentException("GeneratedAt is too old.", nameof(value));
        Value = value.ToUniversalTime();
    }

    public DateTimeOffset Value { get; }

    public override string ToString() => Value.ToString("O");
}
