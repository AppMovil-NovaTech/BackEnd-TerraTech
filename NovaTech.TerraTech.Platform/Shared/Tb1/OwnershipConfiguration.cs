using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Shared.Tb1;
public static class OwnershipConfiguration
{
    public static int UserId(this ClaimsPrincipal principal) => int.TryParse((principal.FindFirstValue("sub") ?? principal.FindFirstValue("sid") ?? principal.FindFirstValue(ClaimTypes.Sid)), out var id) ? id : 0;
    public static void ConfigureTb1(this ModelBuilder b, AppDbContext db)
    {
        b.Entity<User>().Property(x => x.FullName).HasMaxLength(150);
        b.Entity<Profile>().Property(x => x.Location).HasMaxLength(250);
        b.Entity<Profile>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<Profile>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Profile>().HasQueryFilter(x => x.UserId == db.CurrentUserId);
        b.Entity<Field>().Property(x => x.CropName).HasMaxLength(100);
        b.Entity<Field>().OwnsOne(x => x.ProfileId, o => o.HasOne<Profile>().WithMany().HasForeignKey(x => x.Value).OnDelete(DeleteBehavior.Restrict));
        b.Entity<Field>().HasQueryFilter(x => db.Set<Profile>().Any(p => p.Id == x.ProfileId.Value));
        b.Entity<Device>().OwnsOne(x => x.FieldId, o => o.HasOne<Field>().WithMany().HasForeignKey(x => x.Value).OnDelete(DeleteBehavior.Restrict));
        b.Entity<Device>().HasQueryFilter(x => db.Set<Field>().Any(f => f.Id == x.FieldId.Value));
        b.Entity<FertilityZone>().HasQueryFilter(x => db.Set<Field>().Any(f => f.Id == x.FieldId));
        b.Entity<Device>().Property(x => x.SensorCode).HasMaxLength(9);
        b.Entity<Device>().Property(x => x.Name).HasMaxLength(100);
        b.Entity<Device>().HasIndex(x => x.SensorCode).IsUnique();
        b.Entity<Device>().OwnsOne(x => x.MacAddress, o => o.HasIndex(x => x.Value).IsUnique());
        b.Entity<SensorCatalogItem>().HasKey(x => x.SensorCode);
        b.Entity<SensorCatalogItem>().Property(x => x.SensorCode).HasMaxLength(9);
        b.Entity<SensorCatalogItem>().Property(x => x.MacAddress).HasMaxLength(17);
        b.Entity<SensorCatalogItem>().HasIndex(x => x.MacAddress).IsUnique();
        b.Entity<Device>().HasOne<SensorCatalogItem>().WithMany().HasForeignKey(x => x.SensorCode).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SensorReading>().HasKey(x => x.Id);
        b.Entity<SensorReading>().HasOne<Device>().WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SensorReading>().HasIndex(x => new { x.DeviceId, x.RecordedAt }).IsUnique();
        b.Entity<SensorReading>().Property(x => x.Source).HasMaxLength(20);
        b.Entity<SensorReading>().HasQueryFilter(x => db.Set<Device>().Any(d => d.Id == x.DeviceId));
        b.Entity<Report>().OwnsOne(x => x.DeviceId, o => o.HasOne<Device>().WithMany().HasForeignKey(x => x.Value).OnDelete(DeleteBehavior.Restrict));
        b.Entity<Report>().HasQueryFilter(x => db.Set<Device>().Any(d => d.Id == x.DeviceId.Value));
        b.Entity<Order>().HasQueryFilter(x => db.Set<Profile>().Any(p => p.Id == x.ProfileId));
        b.Entity<Notification>().HasQueryFilter(x => db.Set<Profile>().Any(p => p.Id == x.ProfileId));
        b.Entity<Inventory>().HasQueryFilter(x => x.OwnerUserId == db.CurrentUserId);
        b.Entity<Inventory>().HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CommunityProfile>().HasQueryFilter(x => x.VisibilityStatus == CommunityManagement.Domain.Model.ValueObjects.VisibilityStatus.Public || db.Set<Profile>().Any(p => p.Id == x.ProfileId));
        b.Entity<Comment>().HasQueryFilter(x => db.Set<CommunityProfile>().Any(c => c.ProfileId == x.TargetProfileId));
        b.Entity<Order>().HasOne<Profile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Notification>().HasOne<Profile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CommunityProfile>().HasOne<Profile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Comment>().HasOne<Profile>().WithMany().HasForeignKey(x => x.AuthorProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Comment>().HasOne<Profile>().WithMany().HasForeignKey(x => x.TargetProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}
