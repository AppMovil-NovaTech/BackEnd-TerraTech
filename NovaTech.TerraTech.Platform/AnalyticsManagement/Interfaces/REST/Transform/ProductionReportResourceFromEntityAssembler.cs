using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Interfaces.REST.Transform;

public static class ProductionReportResourceFromEntityAssembler
{
    public static ProductionReportResource ToResourceFromEntity(Report report)
    {
        return new ProductionReportResource(
            report.Id,
            report.FieldId ?? 0,
            report.CropName ?? "Unspecified",
            report.HistoricalKg,
            report.ProjectedYieldKg,
            report.ConfidenceScore,
            new CalculationDetailsResource(
                report.HistoricalKg,
                report.EnvironmentalFactor,
                report.FactorReason ?? report.TechnicalInterpretation.Value),
            report.GeneratedAt.Value);
    }
}
