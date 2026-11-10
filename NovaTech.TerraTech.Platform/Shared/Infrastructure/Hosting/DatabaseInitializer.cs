using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Tb1;

namespace NovaTech.TerraTech.Platform.Shared.Infrastructure.Hosting;
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Database.IsNpgsql())
        {
            await db.Database.MigrateAsync(cancellationToken);
            return;
        }

        var configuration = new MySqlConnectionStringBuilder(db.Database.GetConnectionString());
        var database = configuration.Database;
        if (string.IsNullOrWhiteSpace(database) || database.Length > 64 || database.Contains('\0'))
            throw new InvalidOperationException("The MySQL connection must specify a valid database name.");
        configuration.Database = "";
        await using var connection = new MySqlConnection(configuration.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        var lockName = "terratech:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database)))[..48];
        await using var acquire = new MySqlCommand("SELECT GET_LOCK(@name, 60)", connection)
        {
            CommandTimeout = 65
        };
        acquire.Parameters.AddWithValue("@name", lockName);
        if (Convert.ToInt32(await acquire.ExecuteScalarAsync(cancellationToken)) != 1)
            throw new InvalidOperationException("Database initialization is busy. Retry startup after the other instance finishes.");
        try
        {
            await using var exists = new MySqlCommand("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = @database", connection);
            exists.Parameters.AddWithValue("@database", database);
            if (Convert.ToInt32(await exists.ExecuteScalarAsync(cancellationToken)) == 0)
            {
                var quotedDatabase = database.Replace("`", "``");
                await using var create = new MySqlCommand($"CREATE DATABASE `{quotedDatabase}` CHARACTER SET utf8mb4", connection);
                await create.ExecuteNonQueryAsync(cancellationToken);
            }

            await MigrationPreflight.Check(db);
            await db.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            await using var release = new MySqlCommand("SELECT RELEASE_LOCK(@name)", connection);
            release.Parameters.AddWithValue("@name", lockName);
            await release.ExecuteScalarAsync(CancellationToken.None);
        }
    }
}
