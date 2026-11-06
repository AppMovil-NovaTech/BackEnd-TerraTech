using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
public static class ModelBuilderExtensions
{
    public static void ConfigureCommunityManagementContext(this ModelBuilder builder)
    {
        builder.Entity<CommunityProfile>().ToTable("CommunityProfiles");
        builder.Entity<CommunityProfile>().HasKey(p => p.Id);
        builder.Entity<CommunityProfile>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<CommunityProfile>().Property(p => p.ProfileId).IsRequired();
        builder.Entity<CommunityProfile>().OwnsOne(p => p.Nickname, n =>
        {
            n.Property<int>("CommunityProfileId").HasColumnName("id");
            n.Property(x => x.Nickname).HasColumnName("nickname").IsRequired().HasMaxLength(100);
        });
        builder.Entity<CommunityProfile>().OwnsOne(p => p.ReputationScore, r =>
        {
            r.Property<int>("CommunityProfileId").HasColumnName("id");
            r.Property(x => x.Score).HasColumnName("reputation_score").IsRequired();
        });
        builder.Entity<CommunityProfile>().OwnsOne(p => p.PublicBio, b =>
        {
            b.Property<int>("CommunityProfileId").HasColumnName("id");
            b.Property(x => x.Bio).HasColumnName("public_bio").HasMaxLength(500);
        });
        builder.Entity<CommunityProfile>().Property(p => p.VisibilityStatus).IsRequired().HasConversion<int>();
        builder.Entity<Comment>().ToTable("Comments");
        builder.Entity<Comment>().HasKey(c => c.Id);
        builder.Entity<Comment>().Property(c => c.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Comment>().Property(c => c.UserId).IsRequired();
        builder.Entity<Comment>().Property(c => c.ProductId).IsRequired();
        builder.Entity<Comment>().Property(c => c.TargetProfileId).IsRequired();
        builder.Entity<Comment>().Property(c => c.Content).IsRequired().HasMaxLength(1000);
        builder.Entity<Comment>().Property(c => c.Rating).IsRequired();
    }
}
