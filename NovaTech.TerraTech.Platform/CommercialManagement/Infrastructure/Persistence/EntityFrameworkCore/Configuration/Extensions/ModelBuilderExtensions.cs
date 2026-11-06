using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
public static class ModelBuilderExtensions
{
    public static void ApplyCommercialConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).IsRequired().ValueGeneratedOnAdd();
            entity.Property(o => o.UserId).IsRequired();
            entity.Property(o => o.ProductId).IsRequired();
            entity.Property(o => o.ProductName).IsRequired().HasMaxLength(255);
            entity.Property(o => o.Quantity).IsRequired();
            entity.Property(o => o.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(o => o.Status).IsRequired().HasConversion<int>();
            entity.Property(o => o.PaymentMethod).IsRequired().HasConversion<int>();
            entity.Property(o => o.IsSubscription).IsRequired();
            entity.Property(o => o.CreatedAt);
            entity.Property(o => o.UpdatedAt);
        });
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(255);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Price).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(p => p.Type).IsRequired().HasMaxLength(50);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.CreatedAt);
            entity.Property(p => p.UpdatedAt);
        });
    }
}
