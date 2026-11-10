using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Xunit;

namespace TerraTech.IntegrationTests;

public class PostgreSqlJourneyTests
{
    [Fact]
    public async Task PostgreSqlMigratesAndServesAuthenticatedJourney()
    {
        var input = Environment.GetEnvironmentVariable("TB1_TEST_POSTGRES")
            ?? throw new InvalidOperationException("Set TB1_TEST_POSTGRES to an isolated PostgreSQL server with CREATE DATABASE permission.");
        var connection = new NpgsqlConnectionStringBuilder(input);
        var database = "terratech_test_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(connection.ConnectionString))
        {
            await admin.OpenAsync();
            await new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin).ExecuteNonQueryAsync();
        }
        connection.Database = database;
        await using var server = new PostgreSqlServer(connection.ConnectionString);
        var client = server.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/users/me")).StatusCode);

        var email = Guid.NewGuid() + "@test.example";
        var registration = new { fullName = "Postgres Farmer", emailAddress = email, password = "test-password", confirmPassword = "test-password" };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/authentication/sign-up", registration)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/authentication/sign-up", registration)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = server.Emails.Verifications[email] })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = email, password = "test-password" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.GetProperty("token").GetString());
        var profile = await client.PutAsJsonAsync("/api/v1/profiles/me", new { fullName = "Postgres Farmer", fundoName = "Farm", contactPhone = "999888777", location = "Lima", sizeM2 = 10000 });
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var profileData = await profile.Content.ReadFromJsonAsync<JsonElement>();
        var field = await client.PostAsJsonAsync("/api/v1/fields", new { profileId = profileData.GetProperty("id").GetInt32(), name = "North", sizeM2 = 5000, soilType = "SANDY", latitude = -12.0, longitude = -77.0, cropName = "Potato" });
        Assert.Equal(HttpStatusCode.Created, field.StatusCode);
        var fieldData = await field.Content.ReadFromJsonAsync<JsonElement>();

        using var scope = server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Add(new SensorCatalogItem { SensorCode = "TT-000001", MacAddress = "02:00:00:00:00:01", IsDemo = true });
        await db.SaveChangesAsync();
        var device = await client.PostAsJsonAsync("/api/v1/devices/register", new { sensorCode = "TT-000001", fieldId = fieldData.GetProperty("id").GetInt32(), name = "Soil", lastSync = "2026-11-10T10:00:00-05:00" });
        Assert.Equal(HttpStatusCode.Created, device.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/fields")).StatusCode);
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private sealed class PostgreSqlServer(string connection) : WebApplicationFactory<Program>
    {
        public FakeAccountEmailSender Emails { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Database:Provider", "PostgreSql");
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
            builder.UseSetting("TokenSettings:Secret", TestServer.Secret);
            builder.ConfigureServices(services => services.AddSingleton<IAccountEmailSender>(Emails));
        }
    }
}
