using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NovaTech.TerraTech.Platform.Iam.Infrastructure.Messaging;
using NovaTech.TerraTech.Platform.Shared.Tb1;
using Xunit;

public sealed class ResendAccountEmailSenderTests
{
    private static AccountEmailOptions Settings() => new()
    {
        ApiKey = "test-only-placeholder",
        From = "TerraTech <test@example.com>",
        PublicBaseUrl = "https://example.com"
    };

    [Fact]
    public async Task VerificationUsesTheTypedResendPayloadAndDoesNotExposeTheTokenInHeaders()
    {
        using var handler = new RecordingEmailHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler);
        var sender = new ResendAccountEmailSender(client, Options.Create(Settings()));
        var token = new string('A', 43);
        await sender.SendVerification("recipient@example.com", token, CancellationToken.None);
        using var document = JsonDocument.Parse(handler.RequestBody!);
        var payload = document.RootElement;
        Assert.Equal("https://api.resend.com/emails", handler.RequestUri!.AbsoluteUri);
        Assert.Equal("TerraTech <test@example.com>", payload.GetProperty("from").GetString());
        Assert.Equal("recipient@example.com", payload.GetProperty("to")[0].GetString());
        Assert.Contains("verify-email#token=" + token, payload.GetProperty("text").GetString());
        Assert.Contains("24 horas", payload.GetProperty("html").GetString());
        Assert.DoesNotContain(token, handler.IdempotencyKey);
    }

    [Fact]
    public async Task RecoveryMapsProviderFailureToUnavailable()
    {
        using var handler = new RecordingEmailHandler(HttpStatusCode.TooManyRequests);
        using var client = new HttpClient(handler);
        var sender = new ResendAccountEmailSender(client, Options.Create(Settings()));
        var error = await Assert.ThrowsAsync<ApiFailure>(() => sender.SendPasswordReset("recipient@example.com", new string('B', 43), CancellationToken.None));
        Assert.Equal(503, error.Status);
        Assert.Equal("EMAIL_UNAVAILABLE", error.Code);
        using var document = JsonDocument.Parse(handler.RequestBody!);
        Assert.Contains("reset-password#token=", document.RootElement.GetProperty("text").GetString());
        Assert.Contains("1 hora", document.RootElement.GetProperty("html").GetString());
    }
}
