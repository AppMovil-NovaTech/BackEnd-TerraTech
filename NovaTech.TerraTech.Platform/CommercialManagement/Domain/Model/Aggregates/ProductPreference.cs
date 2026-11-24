namespace NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;

/// <summary>US31: opt-in subscription to new products of a specific published type.</summary>
public sealed class ProductPreference
{
    private ProductPreference() { }

    public ProductPreference(int userId, string category)
    {
        if (userId <= 0) throw new ArgumentException("Invalid user identifier.", nameof(userId));
        Category = NormalizeCategory(category);
        UserId = userId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static string NormalizeCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 50)
            throw new ArgumentException("Category must contain 1 to 50 characters.", nameof(value));
        return value.Trim().ToLowerInvariant();
    }
}
