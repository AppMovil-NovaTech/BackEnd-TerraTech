using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.StockManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Inventory resource")]
public record InventoryResource(
    [SwaggerParameter(Description = "Inventory identifier")] int Id,
    [SwaggerParameter(Description = "Product identifier")] int ProductId,
    [SwaggerParameter(Description = "Field identifier")] int FieldId,
    [SwaggerParameter(Description = "Profile identifier")] int ProfileId,
    [SwaggerParameter(Description = "Product name")] string Name,
    [SwaggerParameter(Description = "Quantity of units")] int Quantity,
    [SwaggerParameter(Description = "Unit packaging type")] string UnitType,
    [SwaggerParameter(Description = "Average weight per unit in kilograms")] decimal AverageWeightPerUnitKg,
    [SwaggerParameter(Description = "Total weight in kilograms")] decimal TotalWeightKg,
    [SwaggerParameter(Description = "Unit sale price")] decimal Price,
    [SwaggerParameter(Description = "Creation timestamp")] DateTimeOffset? CreatedAt);
