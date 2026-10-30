using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Xunit;

public partial class JourneyTests
{
    [Fact]
    public async Task MigratingHistoricalAccountsPreservesTheirVerificationExemptionAndData()
    {
        var connection = new MySqlConnectionStringBuilder(server.Connection) {
            Database = "terratech_tb1_legacy_" + Guid.NewGuid().ToString("N")
        };
        await using var mysql = new MySqlConnection(connection.ConnectionString);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection.ConnectionString).Options;
        await using var db = new AppDbContext(options);
        await db.GetService<IMigrator>().MigrateAsync("20261028140145_AddFertilityZoneThresholds");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO users (email_address,password_hash,full_name) VALUES ('historical@example.com','historical-hash','Historical Farmer')");
        await db.GetService<IMigrator>().MigrateAsync();
        var user = await db.Set<User>().SingleAsync();
        Assert.True(user.EmailVerificationExempt);
        Assert.Null(user.EmailVerifiedAtUtc);
        Assert.Equal("Historical Farmer", user.FullName);
        Assert.Equal("historical-hash", user.PasswordHash);
        Assert.Equal(0, user.TokenVersion);
    }
}
