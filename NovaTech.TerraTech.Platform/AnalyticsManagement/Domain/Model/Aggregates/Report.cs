using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
public partial class Report
{
    protected Report()
    {
        DeviceId = null!;
        GeneratedAt = null!;
        MeanValue = null!;
        Variance = null!;
        StandardDeviation = null!;
        TechnicalInterpretation = null!;
    }

    public Report(CreateReportCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        DeviceId = command.DeviceId;
        GeneratedAt = command.GeneratedAt;
        MeanValue = command.MeanValue;
        Variance = command.Variance;
        StandardDeviation = command.StandardDeviation;
        TechnicalInterpretation = command.TechnicalInterpretation;
    }

    public Report(
        int fieldId,
        int deviceId,
        string cropName,
        decimal historicalKg,
        decimal projectedYieldKg,
        double confidenceScore,
        decimal environmentalFactor,
        string factorReason,
        double meanMoisture,
        DateTimeOffset generatedAt)
    {
        FieldId = fieldId;
        DeviceId = new DeviceId(deviceId);
        CropName = cropName;
        HistoricalKg = historicalKg;
        ProjectedYieldKg = projectedYieldKg;
        ConfidenceScore = confidenceScore;
        EnvironmentalFactor = environmentalFactor;
        FactorReason = factorReason;
        GeneratedAt = new GeneratedAt(generatedAt);
        MeanValue = new MeanValue(Math.Clamp(meanMoisture, 0, 100));
        Variance = new Variance(0);
        StandardDeviation = new StandardDeviation(0);
        TechnicalInterpretation = new TechnicalInterpretation(factorReason);
    }

    public int Id { get; private set; }
    public int? FieldId { get; private set; }
    public string? CropName { get; private set; }
    public decimal HistoricalKg { get; private set; }
    public decimal ProjectedYieldKg { get; private set; }
    public double ConfidenceScore { get; private set; }
    public decimal EnvironmentalFactor { get; private set; }
    public string? FactorReason { get; private set; }
    public DeviceId DeviceId { get; private set; }
    public GeneratedAt GeneratedAt { get; private set; }
    public MeanValue MeanValue { get; private set; }
    public Variance Variance { get; private set; }
    public StandardDeviation StandardDeviation { get; private set; }
    public TechnicalInterpretation TechnicalInterpretation { get; private set; }

    public void UpdateStatistics(double mean, double variance, double stdDev, string interpretation)
    {
        MeanValue = new MeanValue(mean);
        Variance = new Variance(variance);
        StandardDeviation = new StandardDeviation(stdDev);
        TechnicalInterpretation = new TechnicalInterpretation(interpretation);
    }

    public void UpdateProjection(
        int deviceId,
        string cropName,
        decimal historicalKg,
        decimal projectedYieldKg,
        double confidenceScore,
        decimal environmentalFactor,
        string factorReason,
        double meanMoisture,
        DateTimeOffset generatedAt)
    {
        DeviceId = new DeviceId(deviceId);
        CropName = cropName;
        HistoricalKg = historicalKg;
        ProjectedYieldKg = projectedYieldKg;
        ConfidenceScore = confidenceScore;
        EnvironmentalFactor = environmentalFactor;
        FactorReason = factorReason;
        GeneratedAt = new GeneratedAt(generatedAt);
        MeanValue = new MeanValue(Math.Clamp(meanMoisture, 0, 100));
        TechnicalInterpretation = new TechnicalInterpretation(factorReason);
    }
}
