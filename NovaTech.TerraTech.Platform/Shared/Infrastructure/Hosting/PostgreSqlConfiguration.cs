using Npgsql;

namespace NovaTech.TerraTech.Platform.Shared.Infrastructure.Hosting;

public static class PostgreSqlConfiguration
{
    public static string ConnectionString(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return new NpgsqlConnectionStringBuilder(value).ConnectionString;

        var uri = new Uri(value);
        var separator = uri.UserInfo.IndexOf(':');
        if (separator < 1 || string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("PostgreSQL URI must specify a host, username and password.");

        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(uri.UserInfo[..separator]),
            Password = Uri.UnescapeDataString(uri.UserInfo[(separator + 1)..]),
            SslMode = SslMode.Require,
            MaxPoolSize = 10,
            Timeout = 10,
            CommandTimeout = 30
        };
        if (string.IsNullOrWhiteSpace(connection.Database))
            throw new InvalidOperationException("PostgreSQL URI must specify a database name.");

        foreach (var parameter in Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query))
        {
            if (!parameter.Key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unsupported PostgreSQL URI parameter. Use an Npgsql connection string for advanced settings.");
            connection["SSL Mode"] = parameter.Value.ToString();
        }

        return connection.ConnectionString;
    }
}
