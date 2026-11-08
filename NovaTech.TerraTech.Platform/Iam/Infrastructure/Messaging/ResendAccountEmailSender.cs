using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using NovaTech.TerraTech.Platform.Iam.Infrastructure.Messaging.Dtos;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Shared.Tb1;

namespace NovaTech.TerraTech.Platform.Iam.Infrastructure.Messaging;

public class ResendAccountEmailSender(HttpClient client, IOptions<AccountEmailOptions> settings) : IAccountEmailSender
{
    private readonly AccountEmailOptions options = settings.Value;
    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.From) ||
            !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var url) || url.Scheme != "https")
            throw Unavailable();
    }

    public Task SendVerification(string email, string token, CancellationToken ct) =>
        Send(email, token, new AccountEmailTemplate(
            Subject: "Verifica tu correo de TerraTech",
            Action: "Verificar correo",
            Page: "verify-email",
            Validity: "24 horas"), ct);

    public Task SendPasswordReset(string email, string token, CancellationToken ct) =>
        Send(email, token, new AccountEmailTemplate(
            Subject: "Restablece tu contraseña de TerraTech",
            Action: "Restablecer contraseña",
            Page: "reset-password",
            Validity: "1 hora"), ct);

    private async Task Send(string email, string token, AccountEmailTemplate template, CancellationToken ct)
    {
        EnsureConfigured();
        var url = $"{options.PublicBaseUrl.TrimEnd('/')}/account/{template.Page}#token={Uri.EscapeDataString(token)}";
        var htmlUrl = HtmlEncoder.Default.Encode(url);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.Add("Idempotency-Key", $"account-{template.Page}-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))}");
        request.Content = JsonContent.Create(new ResendEmailRequestDto(
            options.From,
            [email],
            template.Subject,
            $"{template.Action}: {url}\nEl enlace vence en {template.Validity}. Si no lo solicitaste, ignora este mensaje.",
            $"<h1>TerraTech</h1><p>{template.Action}</p><p><a href=\"{htmlUrl}\">{template.Action}</a></p><p>El enlace vence en {template.Validity}. Si no lo solicitaste, ignora este mensaje.</p>"
        ));
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw Unavailable();
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
    }

    private static ApiFailure Unavailable() => new(503, "EMAIL_UNAVAILABLE", "Email delivery is unavailable. Please try again later.");
}
