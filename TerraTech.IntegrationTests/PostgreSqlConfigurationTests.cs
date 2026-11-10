using Microsoft.EntityFrameworkCore;
using Npgsql;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Hosting;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Tb1;
using Xunit;

namespace TerraTech.IntegrationTests;

public class PostgreSqlConfigurationTests
{
    [Fact]
    public void SupabaseUriPreservesEncodedCredentialsAndRequiresTls()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgreSqlConfiguration.ConnectionString(
            "postgresql://postgres:sample%3Bpassword%40host@db.example.com:5432/postgres"));
        Assert.Equal("sample;password@host", connection.Password);
        Assert.Equal("db.example.com", connection.Host);
        Assert.Equal("postgres", connection.Database);
        Assert.Equal(SslMode.Require, connection.SslMode);
        Assert.Equal(10, connection.MaxPoolSize);
    }

    [Fact]
    public void SessionPoolerConnectionPreservesProjectUsername()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgreSqlConfiguration.ConnectionString(
            "Host=pooler.example.com;Port=5432;Database=postgres;Username=postgres.project;Password=sample;SSL Mode=Require"));
        Assert.Equal("postgres.project", connection.Username);
        Assert.Equal(5432, connection.Port);
    }

    [Theory]
    [InlineData("postgresql://postgres@db.example.com/postgres")]
    [InlineData("postgresql://postgres:sample@db.example.com/")]
    [InlineData("postgresql://postgres:sample@db.example.com/postgres?unknown=true")]
    public void InvalidUrisFailWithoutLeakingCredentials(string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => PostgreSqlConfiguration.ConnectionString(value));
        Assert.DoesNotContain("sample", error.Message);
    }

    [Fact]
    public void PostgreSqlUsesSeparateMigrationsAndPrivateSchema()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlAppDbContext>()
            .UseNpgsql("Host=localhost;Database=terratech_design;Username=design");
        using var db = new PostgreSqlAppDbContext(options.Options);
        Assert.Single(db.Database.GetMigrations());
        var sql = db.Database.GenerateCreateScript();
        Assert.Contains("terratech.users", sql);
        Assert.DoesNotContain("longtext", sql);
        Assert.DoesNotContain("AUTO_INCREMENT", sql);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, true)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, false)]
    public void PostgreSqlConstraintErrorsPreserveConflictHandling(string code, bool unique)
    {
        var exception = new DbUpdateException("Database failure", new PostgresException("conflict", "ERROR", "ERROR", code));
        Assert.Equal(unique, DatabaseErrors.IsUniqueViolation(exception));
        Assert.True(DatabaseErrors.IsConstraintViolation(exception));
    }
}
