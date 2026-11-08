namespace NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Commands;
public record CreateInventoryCommand(
    string Name,
    int FieldId,
    int ProfileId,
    int Quantity,
    string UnitType,
    decimal AverageWeightPerUnitKg,
    decimal Price,
    string ImageUrl);
