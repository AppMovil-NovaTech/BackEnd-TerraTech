using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Interfaces.REST.Resources;

[SwaggerSchema(Description = "Details of the yield projection calculation")]
public record CalculationDetailsResource(
    [SwaggerParameter(Description = "Historical total weight in kilograms")] decimal HistoricalTotalKg,
    [SwaggerParameter(Description = "Environmental factor multiplier (0.85, 1.00, 1.05)")] decimal EnvironmentalFactor,
    [SwaggerParameter(Description = "Reason for the applied environmental factor")] string FactorReason);

[SwaggerSchema(Description = "Agricultural yield projection report resource")]
public record ProductionReportResource(
    [SwaggerParameter(Description = "Report ID")] int Id,
    [SwaggerParameter(Description = "Field ID")] int FieldId,
    [SwaggerParameter(Description = "Crop name")] string CropName,
    [SwaggerParameter(Description = "Cumulative historical weight in kilograms")] decimal HistoricalKg,
    [SwaggerParameter(Description = "Projected yield in kilograms")] decimal ProjectedYieldKg,
    [SwaggerParameter(Description = "Confidence score (0.0 to 1.0)")] double ConfidenceScore,
    [SwaggerParameter(Description = "Calculation details")] CalculationDetailsResource CalculationDetails,
    [SwaggerParameter(Description = "Generation timestamp")] DateTimeOffset GeneratedAt);
