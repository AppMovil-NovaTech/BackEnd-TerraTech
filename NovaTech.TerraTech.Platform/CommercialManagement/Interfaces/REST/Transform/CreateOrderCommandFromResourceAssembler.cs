using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Transform;
public static class CreateOrderCommandFromResourceAssembler
{
    public static CreateOrderCommand ToCommandFromResource(CreateOrderResource resource)
    {
        return new CreateOrderCommand(resource.UserId, resource.ProductId, resource.Quantity, resource.PaymentMethod, resource.IsSubscription);
    }
}
