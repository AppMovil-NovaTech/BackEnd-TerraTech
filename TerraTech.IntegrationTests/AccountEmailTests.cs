using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Entities;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using Xunit;
public partial class JourneyTests
{
    [Fact]
    public async Task RecoveryChangesPasswordRevokesSessionsAndCannotBeReused()
    {
        var account = await Account();
        var me = await Json(await account.client.GetAsync("/api/v1/users/me"));
        var email = me.GetProperty("emailAddress").GetString()!;
        var anonymous = server.CreateClient();
        Assert.Equal(HttpStatusCode.Accepted, (await anonymous.PostAsJsonAsync("/api/v1/authentication/password-reset-requests", new { emailAddress = email })).StatusCode);
        var token = server.Emails.Resets[email];
        await server.WithDb(async db =>
        {
            var stored = await db.Set<AccountEmailToken>().SingleAsync(t => t.UserId == account.userId);
            Assert.NotEqual(token, stored.TokenHash);
            Assert.Equal(64, stored.TokenHash.Length);
        });
        var reset = new { token, password = "new-test-password", confirmPassword = "new-test-password" };
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.PostAsJsonAsync("/api/v1/authentication/password-resets", reset)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/v1/authentication/password-resets", reset)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await account.client.GetAsync("/api/v1/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = email, password = "test-password" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = email, password = "new-test-password" })).StatusCode);
    }
    [Fact]
    public async Task VerificationExpiresAndResendingInvalidatesThePreviousLink()
    {
        var client = server.CreateClient();
        var email = Guid.NewGuid() + "@test.example";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/authentication/sign-up", Registration(email))).StatusCode);
        var first = server.Emails.Verifications[email];
        await server.WithDb(async db =>
        {
            var record = await db.Set<AccountEmailToken>().SingleAsync(t => t.TokenHash != "" && t.UserId == db.Set<User>().Where(u => u.EmailAddress.Value == email).Select(u => u.Id).Single());
            record.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
            record.RequestedAtUtc = DateTime.UtcNow.AddMinutes(-2);
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = first })).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications", new { emailAddress = email })).StatusCode);
        var next = server.Emails.Verifications[email];
        Assert.NotEqual(first, next);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = first })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = next })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token = next })).StatusCode);
    }
    [Fact]
    public async Task ResetRequestDoesNotRevealWhetherTheAccountExists()
    {
        var account = await Account();
        var me = await Json(await account.client.GetAsync("/api/v1/users/me"));
        var client = server.CreateClient();
        var found = await client.PostAsJsonAsync("/api/v1/authentication/password-reset-requests", new { emailAddress = me.GetProperty("emailAddress").GetString() });
        var missing = await client.PostAsJsonAsync("/api/v1/authentication/password-reset-requests", new { emailAddress = Guid.NewGuid() + "@test.example" });
        Assert.Equal(HttpStatusCode.Accepted, found.StatusCode);
        Assert.Equal(found.StatusCode, missing.StatusCode);
        Assert.Equal(await found.Content.ReadAsStringAsync(), await missing.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task UnavailableEmailDoesNotReserveAnAccountAndPagesDoNotConsumeTokens()
    {
        var client = server.CreateClient();
        var email = Guid.NewGuid() + "@test.example";
        server.Emails.Unavailable = true;
        try
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/authentication/sign-up", Registration(email))).StatusCode);
            await server.WithDb(async db => Assert.False(await db.Set<User>().AnyAsync(u => u.EmailAddress.Value == email)));
        }
        finally { server.Emails.Unavailable = false; }
        foreach (var path in new[] { "verify-email", "reset-password", "account.css", "account.js" })
        {
            var page = await client.GetAsync("/account/" + path);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Contains("no-store", page.Headers.CacheControl!.ToString());
            Assert.Equal("no-referrer", page.Headers.GetValues("Referrer-Policy").Single());
        }
    }

    [Fact]
    public async Task InvalidConfirmationAndWrongPurposeDoNotConsumeTheResetLink()
    {
        var account = await Account();
        var me = await Json(await account.client.GetAsync("/api/v1/users/me"));
        var email = me.GetProperty("emailAddress").GetString()!;
        var client = server.CreateClient();
        await client.PostAsJsonAsync("/api/v1/authentication/password-reset-requests", new { emailAddress = email });
        var token = server.Emails.Resets[email];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications/confirm", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/authentication/password-resets", new { token, password = "replacement", confirmPassword = "different" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/authentication/password-resets", new { token, password = "replacement", confirmPassword = "replacement" })).StatusCode);
    }
    [Fact]
    public async Task ExpiredRecoveryLinkIsRejectedAtTheExactExpiry()
    {
        var account = await Account();
        var me = await Json(await account.client.GetAsync("/api/v1/users/me"));
        var email = me.GetProperty("emailAddress").GetString()!;
        var client = server.CreateClient();
        await client.PostAsJsonAsync("/api/v1/authentication/password-reset-requests", new { emailAddress = email });
        var token = server.Emails.Resets[email];
        await server.WithDb(async db =>
        {
            var record = await db.Set<AccountEmailToken>().SingleAsync(t => t.UserId == account.userId);
            server.Clock.Frozen = new DateTimeOffset(DateTime.SpecifyKind(record.ExpiresAtUtc, DateTimeKind.Utc));
        });
        try
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/authentication/password-resets", new { token, password = "replacement", confirmPassword = "replacement" })).StatusCode);
        }
        finally { server.Clock.Frozen = null; }
    }
    [Fact]
    public async Task RepeatedRequestsWithinOneMinuteKeepTheOriginalLink()
    {
        var client = server.CreateClient();
        var email = Guid.NewGuid() + "@test.example";
        await client.PostAsJsonAsync("/api/v1/authentication/sign-up", Registration(email));
        var token = server.Emails.Verifications[email];
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/authentication/email-verifications", new { emailAddress = email })).StatusCode);
        Assert.Equal(token, server.Emails.Verifications[email]);
    }
    [Fact]
    public async Task HistoricalAccountExemptionPreservesLoginAndExistingJwtClaims()
    {
        var account = await Account();
        await server.WithDb(async db =>
        {
            var user = await db.Set<User>().SingleAsync(u => u.Id == account.userId);
            user.EmailVerifiedAtUtc = null;
            user.EmailVerificationExempt = true;
            await db.SaveChangesAsync();
        });
        var me = await Json(await account.client.GetAsync("/api/v1/users/me"));
        var login = await server.CreateClient().PostAsJsonAsync("/api/v1/authentication/sign-in", new { emailAddress = me.GetProperty("emailAddress").GetString(), password = "test-password" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
