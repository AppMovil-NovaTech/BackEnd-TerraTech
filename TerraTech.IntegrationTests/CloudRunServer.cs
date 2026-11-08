using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class CloudRunServer(string connection, bool migrateOnStartup = false) : WebApplicationFactory<Program>
{
    public FakeAccountEmailSender Emails { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Email:RequestsPerMinute", "10000");
        builder.ConfigureServices(services => services.AddSingleton<IAccountEmailSender>(Emails));
        builder.UseSetting("Database:MigrateOnStartup", migrateOnStartup.ToString());
        builder.UseSetting("K_SERVICE", "terratech-test");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
        builder.UseSetting("TokenSettings:Secret", TestServer.Secret);
    }
}
