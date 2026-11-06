using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.StockManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.StockManagement.Interfaces.REST.Transform;
public static class CreateInventoryCommandFromResourceAssembler
{
    public static CreateInventoryCommand ToCommandFromResource(CreateInventoryResource resource)
    {
        return new CreateInventoryCommand(
            resource.Name,
            resource.FieldId,
            resource.ProfileId,
            resource.Quantity,
            resource.UnitType,
            resource.AverageWeightPerUnitKg,
            resource.Price,
            resource.ImageUrl);
    }
}
