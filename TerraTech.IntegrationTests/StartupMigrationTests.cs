using System.Net;
using System.Net.Http.Json;
using MySql.Data.MySqlClient;
using Xunit;

public partial class JourneyTests
{
    [Fact]
    public async Task CloudRunCreatesMissingDatabaseAndSchemaAndPreservesAccountsOnRestart()
    {
        var connection = new MySqlConnectionStringBuilder(server.Connection)
        {
            Database = "terratech_tb1_" + Guid.NewGuid().ToString("N")
        };
        int id;
        await using (var production = new CloudRunServer(connection.ConnectionString, migrateOnStartup: true))
        {
            var client = production.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
            var response = await client.PostAsJsonAsync("/api/v1/authentication/sign-up", Registration("startup@test.example"));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            id = (await Json(response)).GetProperty("id").GetInt32();
        }

        await using var restarted = new CloudRunServer(connection.ConnectionString, migrateOnStartup: true);
        var restartClient = restarted.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await restartClient.GetAsync("/health/ready")).StatusCode);
        var login = await restartClient.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = "startup@test.example", password = "test-password" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(id, (await Json(login)).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task ConcurrentCloudRunStartsCreateOneCompleteSchema()
    {
        var connection = new MySqlConnectionStringBuilder(server.Connection)
        {
            Database = "terratech_tb1_" + Guid.NewGuid().ToString("N")
        };
        await using var first = new CloudRunServer(connection.ConnectionString, migrateOnStartup: true);
        await using var second = new CloudRunServer(connection.ConnectionString, migrateOnStartup: true);
        var clients = await Task.WhenAll(Task.Run(() => first.CreateClient()), Task.Run(() => second.CreateClient()));
        foreach (var client in clients)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        await using var mysql = new MySqlConnection(connection.ConnectionString);
        await mysql.OpenAsync();
        var count = await new MySqlCommand("SELECT COUNT(*) FROM __EFMigrationsHistory", mysql).ExecuteScalarAsync();
        Assert.Equal(5, Convert.ToInt32(count));
    }
}
