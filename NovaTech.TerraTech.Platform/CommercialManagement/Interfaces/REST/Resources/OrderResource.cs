using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.ValueObjects;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Order resource")]
public record OrderResource(
    [SwaggerParameter(Description = "Order identifier")] int Id,
    [SwaggerParameter(Description = "User identifier")] int UserId,
    [SwaggerParameter(Description = "Product identifier")] int ProductId,
    [SwaggerParameter(Description = "Product name")] string ProductName,
    [SwaggerParameter(Description = "Quantity")] int Quantity,
    [SwaggerParameter(Description = "Total amount")] decimal TotalAmount,
    [SwaggerParameter(Description = "Order status")] OrderStatus Status,
    [SwaggerParameter(Description = "Payment method")] PaymentMethod PaymentMethod,
    [SwaggerParameter(Description = "Creation timestamp")] DateTimeOffset CreatedAt,
    [SwaggerParameter(Description = "Is subscription")] bool IsSubscription);
