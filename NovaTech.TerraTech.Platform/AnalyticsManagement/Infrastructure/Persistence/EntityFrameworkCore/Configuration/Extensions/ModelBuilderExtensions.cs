using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
public static class ModelBuilderExtensions
{
    public static void ApplyAnalyticsManagementConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Report>(report =>
        {
            report.HasKey(r => r.Id);
            report.Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
            report.Property(r => r.FieldId).HasColumnType("int");
            report.Property(r => r.CropName).HasMaxLength(150);
            report.Property(r => r.HistoricalKg).HasColumnType("decimal(18,2)");
            report.Property(r => r.ProjectedYieldKg).HasColumnType("decimal(18,2)");
            report.Property(r => r.ConfidenceScore).HasColumnType("double");
            report.Property(r => r.EnvironmentalFactor).HasColumnType("decimal(18,2)");
            report.Property(r => r.FactorReason).HasMaxLength(500);
            report.OwnsOne(r => r.DeviceId, device =>
            {
                device.WithOwner().HasForeignKey("Id");
                device.Property(d => d.Value).HasColumnName("DeviceId").IsRequired();
            });
            report.OwnsOne(r => r.GeneratedAt, gen =>
            {
                gen.WithOwner().HasForeignKey("Id");
                gen.Property(g => g.Value).HasColumnName("GeneratedAt").IsRequired();
            });
            report.OwnsOne(r => r.MeanValue, mean =>
            {
                mean.WithOwner().HasForeignKey("Id");
                mean.Property(m => m.Value).HasColumnName("MeanValue").IsRequired();
            });
            report.OwnsOne(r => r.Variance, variance =>
            {
                variance.WithOwner().HasForeignKey("Id");
                variance.Property(v => v.Value).HasColumnName("Variance").IsRequired();
            });
            report.OwnsOne(r => r.StandardDeviation, stdDev =>
            {
                stdDev.WithOwner().HasForeignKey("Id");
                stdDev.Property(s => s.Value).HasColumnName("StandardDeviation").IsRequired();
            });
            report.OwnsOne(r => r.TechnicalInterpretation, interpretation =>
            {
                interpretation.WithOwner().HasForeignKey("Id");
                interpretation.Property(t => t.Value).HasColumnName("TechnicalInterpretation").HasMaxLength(500).IsRequired();
            });
        });
    }
}
