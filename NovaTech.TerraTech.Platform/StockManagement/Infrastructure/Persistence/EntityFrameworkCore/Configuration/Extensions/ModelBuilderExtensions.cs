using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.StockManagement.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
public static class ModelBuilderExtensions
{
    public static void ApplyStockConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Inventory>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).IsRequired().ValueGeneratedOnAdd();
            entity.Property(i => i.ProductId).IsRequired().HasColumnType("int");
            entity.Property(i => i.FieldId).IsRequired().HasColumnType("int");
            entity.Property(i => i.ProfileId).IsRequired().HasColumnType("int");
            entity.Property(i => i.Name).IsRequired().HasMaxLength(255);
            entity.Property(i => i.StockQuantity).IsRequired().IsConcurrencyToken();
            entity.Property(i => i.UnitType).IsRequired().HasMaxLength(50);
            entity.Property(i => i.AverageWeightPerUnitKg).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(i => i.TotalWeightKg).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(i => i.Price).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(i => i.CreatedAt);
            entity.Property(i => i.UpdatedAt);
        });
    }
}
