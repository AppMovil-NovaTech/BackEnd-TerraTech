using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Shared.Tb1;
using Xunit;

public class TestServer : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Secret = "Tb1IntegrationOnlyRandomKeyWithAtLeast32Bytes2026ExtraEntropyForJwtAlgorithmTests";
    public string Connection { get; private set; } = null!;
    public FakeAccountEmailSender Emails { get; } = new();
    public ControlledClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Email:RequestsPerMinute", "10000");
        builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(Clock).AddSingleton<NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices.IAccountEmailSender>(Emails));
    }

    public async Task InitializeAsync()
    {
        var input = Environment.GetEnvironmentVariable("TB1_TEST_MYSQL") ?? throw new InvalidOperationException("Set TB1_TEST_MYSQL for an isolated MySQL server.");
        var config = new MySqlConnectionStringBuilder(input);
        config.Database = "terratech_tb1_" + Guid.NewGuid().ToString("N");
        Connection = config.ConnectionString;
        var database = config.Database;
        config.Database = "";
        await using var mysql = new MySqlConnection(config.ConnectionString);
        await mysql.OpenAsync();
        await new MySqlCommand($"CREATE DATABASE `{database}`", mysql).ExecuteNonQueryAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", Connection);
        Environment.SetEnvironmentVariable("TokenSettings__Secret", Secret);
        _ = CreateClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }

    public async Task WithDb(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
