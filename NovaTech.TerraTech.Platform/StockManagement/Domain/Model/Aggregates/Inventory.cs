using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Commands;

namespace NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;
public partial class Inventory
{
    protected Inventory()
    {
        Name = string.Empty;
        UnitType = string.Empty;
    }

    public Inventory(CreateInventoryCommand command, int productId)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (productId <= 0)
            throw new ArgumentException("ProductId must be greater than zero", nameof(productId));
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Product name is required", nameof(command));
        if (command.FieldId <= 0)
            throw new ArgumentException("FieldId must be greater than zero", nameof(command));
        if (command.ProfileId <= 0)
            throw new ArgumentException("ProfileId must be greater than zero", nameof(command));
        if (command.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(command));
        if (string.IsNullOrWhiteSpace(command.UnitType))
            throw new ArgumentException("UnitType is required", nameof(command));
        if (command.AverageWeightPerUnitKg <= 0)
            throw new ArgumentException("AverageWeightPerUnitKg must be greater than zero", nameof(command));
        if (command.Price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(command));

        ProductId = productId;
        FieldId = command.FieldId;
        ProfileId = command.ProfileId;
        Name = command.Name.Trim();
        StockQuantity = command.Quantity;
        UnitType = command.UnitType.Trim();
        AverageWeightPerUnitKg = command.AverageWeightPerUnitKg;
        TotalWeightKg = command.Quantity * command.AverageWeightPerUnitKg;
        Price = command.Price;
    }

    public int? OwnerUserId { get; private set; }

    public void AssignOwner(int userId) => OwnerUserId = userId;
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public int FieldId { get; private set; }
    public int ProfileId { get; private set; }
    public string Name { get; private set; }
    public int StockQuantity { get; private set; }
    public string UnitType { get; private set; }
    public decimal AverageWeightPerUnitKg { get; private set; }
    public decimal TotalWeightKg { get; private set; }
    public decimal Price { get; private set; }

    public void UpdateStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(newQuantity));
        StockQuantity = newQuantity;
        TotalWeightKg = newQuantity * AverageWeightPerUnitKg;
    }

    public void DiscountStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Discount quantity must be greater than zero", nameof(quantity));
        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock");
        StockQuantity -= quantity;
    }

    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Restore quantity must be greater than zero", nameof(quantity));
        StockQuantity += quantity;
    }
}
