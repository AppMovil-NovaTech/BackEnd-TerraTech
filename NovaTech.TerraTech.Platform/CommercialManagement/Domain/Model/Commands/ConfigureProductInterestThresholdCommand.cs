namespace NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
public record ConfigureProductInterestThresholdCommand(int ProductId, int Threshold, int UserId);
