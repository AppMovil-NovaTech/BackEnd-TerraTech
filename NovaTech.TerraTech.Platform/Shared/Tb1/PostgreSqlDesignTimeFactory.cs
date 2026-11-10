using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Hosting;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Shared.Tb1;

public class PostgreSqlDesignTimeFactory : IDesignTimeDbContextFactory<PostgreSqlAppDbContext>
{
    public PostgreSqlAppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=terratech_design;Username=design";
        var options = new DbContextOptionsBuilder<PostgreSqlAppDbContext>()
            .UseNpgsql(PostgreSqlConfiguration.ConnectionString(connection),
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "terratech"));
        return new PostgreSqlAppDbContext(options.Options);
    }
}
