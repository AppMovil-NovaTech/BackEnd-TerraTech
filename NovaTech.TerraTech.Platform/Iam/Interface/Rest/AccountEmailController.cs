using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.CommandServices;
using NovaTech.TerraTech.Platform.Iam.Interface.Rest.Resources;
namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest;

[ApiController, Route("api/v1/authentication"), AllowAnonymous, EnableRateLimiting("account-email")]
public class AccountEmailController(AccountEmailService emails) : ControllerBase
{
    [HttpPost("email-verifications"), ProducesResponseType(202), ProducesResponseType(503)]
    public async Task<IActionResult> RequestVerification(AccountEmailRequest request, CancellationToken ct)
    {
        await emails.RequestVerification(request.EmailAddress, ct);
        return Accepted(new { message = "If verification is required, an email will be sent. Please check your inbox." });
    }
    [HttpPost("email-verifications/confirm"), ProducesResponseType(204), ProducesResponseType(400)]
    public async Task<IActionResult> ConfirmVerification(ConfirmEmailResource request, CancellationToken ct)
    {
        await emails.ConfirmVerification(request.Token, ct);
        return NoContent();
    }
    [HttpPost("password-reset-requests"), ProducesResponseType(202), ProducesResponseType(503)]
    public async Task<IActionResult> RequestPasswordReset(AccountEmailRequest request, CancellationToken ct)
    {
        await emails.RequestPasswordReset(request.EmailAddress, ct);
        return Accepted(new { message = "If the account exists, an email will be sent. Please check your inbox." });
    }
    [HttpPost("password-resets"), ProducesResponseType(204), ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword(ResetPasswordResource request, CancellationToken ct)
    {
        await emails.ResetPassword(request.Token, request.Password, request.ConfirmPassword, ct);
        return NoContent();
    }
}
