namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
public record ProductInterestResource(
    int ProductId,
    int InterestThreshold,
    int MonthlyConsultations,
    int ConsultationMonth,
    int ConsultationYear,
    DateTimeOffset? LastConsultationAt);
