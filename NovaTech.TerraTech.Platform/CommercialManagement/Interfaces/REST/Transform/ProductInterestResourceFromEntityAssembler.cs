using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Transform;
public static class ProductInterestResourceFromEntityAssembler
{
    public static ProductInterestResource ToResourceFromEntity(Product product)
    {
        return new ProductInterestResource(
            product.Id,
            product.InterestThreshold,
            product.MonthlyConsultations,
            product.ConsultationMonth,
            product.ConsultationYear,
            product.LastConsultationAt);
    }
}
