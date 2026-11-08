using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.StockManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.StockManagement.Interfaces.REST.Transform;
public static class InventoryResourceFromEntityAssembler
{
    public static InventoryResource ToResourceFromEntity(Inventory entity)
    {
        return new InventoryResource(
            entity.Id,
            entity.ProductId,
            entity.FieldId,
            entity.ProfileId,
            entity.Name,
            entity.StockQuantity,
            entity.UnitType,
            entity.AverageWeightPerUnitKg,
            entity.TotalWeightKg,
            entity.Price,
            entity.CreatedAt);
    }
}
