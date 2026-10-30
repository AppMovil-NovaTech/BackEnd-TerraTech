using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using Xunit;

public partial class JourneyTests
{
    [Fact]
    public async Task ConcurrentRecoveryConsumesTheTokenExactlyOnce()
    {
        var account = await Account();
        var user = await Json(await account.client.GetAsync("/api/v1/users/me"));
        var email = user.GetProperty("emailAddress").GetString()!;
        var client = server.CreateClient();
        await client.PostAsJsonAsync("/api/v1/authentication/password-reset-requests", new { emailAddress = email });
        var payload = new { token = server.Emails.Resets[email], password = "replacement-password", confirmPassword = "replacement-password" };
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/authentication/password-resets", payload),
            client.PostAsJsonAsync("/api/v1/authentication/password-resets", payload));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.BadRequest);
        await server.WithDb(async db => Assert.Equal(1, (await db.Set<User>().SingleAsync(item => item.Id == account.userId)).TokenVersion));
    }
}
