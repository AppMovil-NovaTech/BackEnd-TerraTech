using System.Collections.Concurrent;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Shared.Tb1;
public sealed class FakeAccountEmailSender : IAccountEmailSender
{
    public ConcurrentDictionary<string, string> Verifications { get; } = new();
    public ConcurrentDictionary<string, string> Resets { get; } = new();
    public bool Unavailable { get; set; }
    public void EnsureConfigured()
    {
        if (Unavailable) throw new ApiFailure(503, "EMAIL_UNAVAILABLE", "Email unavailable.");
    }
    public Task SendVerification(string email, string token, CancellationToken ct)
    {
        EnsureConfigured(); Verifications[email] = token; return Task.CompletedTask;
    }
    public Task SendPasswordReset(string email, string token, CancellationToken ct)
    {
        EnsureConfigured(); Resets[email] = token; return Task.CompletedTask;
    }
}
