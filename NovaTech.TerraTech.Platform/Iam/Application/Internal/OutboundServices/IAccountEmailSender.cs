namespace NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;

public interface IAccountEmailSender
{
    void EnsureConfigured();
    Task SendVerification(string email, string token, CancellationToken ct);
    Task SendPasswordReset(string email, string token, CancellationToken ct);
}
