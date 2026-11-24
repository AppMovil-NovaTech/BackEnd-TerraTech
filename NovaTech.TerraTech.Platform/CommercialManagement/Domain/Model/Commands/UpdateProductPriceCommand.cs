namespace NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
public record UpdateProductPriceCommand(int ProductId, int UserId, decimal Price);
