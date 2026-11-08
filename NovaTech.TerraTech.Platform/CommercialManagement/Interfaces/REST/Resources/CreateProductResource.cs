using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Request payload to create a product")]
public record CreateProductResource([Required][SwaggerParameter(Description = "Product name")] string Name, [SwaggerParameter(Description = "Product description")] string Description, [Required][SwaggerParameter(Description = "Product price")] decimal Price, [Required][SwaggerParameter(Description = "Product type")] string Type, [Required][SwaggerParameter(Description = "Product image URL")] string ImageUrl);
