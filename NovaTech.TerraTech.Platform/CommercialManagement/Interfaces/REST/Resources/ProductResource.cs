using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Product resource")]
public record ProductResource([SwaggerParameter(Description = "Product identifier")] int Id, [SwaggerParameter(Description = "Product name")] string Name, [SwaggerParameter(Description = "Product description")] string Description, [SwaggerParameter(Description = "Product price")] decimal Price, [SwaggerParameter(Description = "Product type")] string Type, [SwaggerParameter(Description = "Product image URL")] string? ImageUrl);
