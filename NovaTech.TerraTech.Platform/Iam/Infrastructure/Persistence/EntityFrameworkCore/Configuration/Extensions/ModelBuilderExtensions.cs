using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Entities;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.Entity<AccountEmailToken>(token =>
        {
            token.ToTable("account_email_tokens");
            token.HasKey(t => new { t.UserId, t.Purpose });
            token.Property(t => t.Purpose).HasConversion<string>().HasMaxLength(32);
            token.Property(t => t.TokenHash).HasMaxLength(64).IsConcurrencyToken();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            user.Property(u => u.Id).ValueGeneratedOnAdd();
            user.OwnsOne(u => u.EmailAddress, email =>
            {
                email.WithOwner().HasForeignKey("Id");
                email.Property(e => e.Value).HasColumnName("email_address").IsRequired().HasMaxLength(255);
                email.HasIndex(e => e.Value).IsUnique();
            });
            user.Property(u => u.PasswordHash).IsRequired();
            user.Property(u => u.CreatedAt);
            user.Property(u => u.UpdatedAt);
        });
    }
}
