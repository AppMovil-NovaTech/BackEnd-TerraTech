using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.StockManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Request payload to create an inventory item")]
public record CreateInventoryResource(
    [Required][SwaggerParameter(Description = "Product name")] string Name,
    [Required][SwaggerParameter(Description = "Field identifier")] int FieldId,
    [Required][SwaggerParameter(Description = "Profile identifier")] int ProfileId,
    [Required][SwaggerParameter(Description = "Quantity of units")] int Quantity,
    [Required][SwaggerParameter(Description = "Unit packaging type")] string UnitType,
    [Required][SwaggerParameter(Description = "Average weight per unit in kilograms")] decimal AverageWeightPerUnitKg,
    [Required][SwaggerParameter(Description = "Unit sale price")] decimal Price,
    [Required][SwaggerParameter(Description = "Product image URL")] string ImageUrl);
