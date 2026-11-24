namespace NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;

/// <summary>A user's subscription to published product price/availability changes (US32).</summary>
public class FavoriteProduct
{
    protected FavoriteProduct() { }

    public FavoriteProduct(int userId, int productId)
    {
        if (userId <= 0 || productId <= 0)
            throw new ArgumentException("Valid user and product identifiers are required.");
        UserId = userId;
        ProductId = productId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int ProductId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
