using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaTech.TerraTech.Platform.Iam.Application.CommandServices;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Commands;
using Xunit;

public partial class JourneyTests
{
    [Fact]
    public async Task LegacyRegistrationAndLoginCannotBypassEmailVerification()
    {
        var email = Guid.NewGuid() + "@test.example";
        using (var scope = server.Services.CreateScope())
        {
            var commands = scope.ServiceProvider.GetRequiredService<IUserCommandService>();
            Assert.True((await commands.Handle(new SignUpCommand(email, "test-password"), CancellationToken.None)).IsSuccess);
            Assert.True((await commands.Handle(new SignInCommand(email, "test-password"), CancellationToken.None)).IsFailure);
        }
        var client = server.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = email, password = "test-password" })).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications", new { emailAddress = email })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = server.Emails.Verifications[email] })).StatusCode);
        using (var scope = server.Services.CreateScope())
        {
            var commands = scope.ServiceProvider.GetRequiredService<IUserCommandService>();
            Assert.True((await commands.Handle(new SignInCommand(email, "test-password"), CancellationToken.None)).IsSuccess);
        }
        await server.WithDb(async db => Assert.False((await db.Set<User>().SingleAsync(user => user.EmailAddress.Value == email)).EmailVerificationExempt));
    }
}
