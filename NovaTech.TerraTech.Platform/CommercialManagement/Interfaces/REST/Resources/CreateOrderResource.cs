using System.ComponentModel.DataAnnotations;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.ValueObjects;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Request payload to create an order")]
public record CreateOrderResource(
    [Required][SwaggerParameter(Description = "User identifier")] int UserId,
    [Required][SwaggerParameter(Description = "Product identifier")] int ProductId,
    [Required][SwaggerParameter(Description = "Quantity")] int Quantity,
    [Required][SwaggerParameter(Description = "Payment method")] PaymentMethod PaymentMethod,
    [SwaggerParameter(Description = "Whether this is a subscription")] bool IsSubscription = false);
