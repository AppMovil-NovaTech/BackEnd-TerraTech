using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
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
        builder.Entity<FavoriteProduct>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id).ValueGeneratedOnAdd();
            entity.Property(f => f.UserId).IsRequired();
            entity.Property(f => f.ProductId).IsRequired();
            entity.Property(f => f.CreatedAt).IsRequired();
            entity.HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();
            entity.HasIndex(f => f.ProductId);
            entity.HasOne<User>()
                .WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ProductPreference>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedOnAdd();
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.Category).IsRequired().HasMaxLength(50);
            entity.Property(p => p.CreatedAt).IsRequired();
            entity.HasIndex(p => new { p.UserId, p.Category }).IsUnique();
            entity.HasIndex(p => p.Category);
            entity.HasOne<User>().WithMany().HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.Property(p => p.InterestThreshold).IsRequired().HasDefaultValue(100);
            entity.Property(p => p.MonthlyConsultations).IsRequired();
            entity.Property(p => p.ConsultationMonth).IsRequired();
            entity.Property(p => p.ConsultationYear).IsRequired();
            entity.Property(p => p.LastConsultationAt);
            entity.Property(p => p.HighInterestAlertSent).IsRequired();
            entity.Property(p => p.InactivityAlertSent).IsRequired();
        });
    }
}
