using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.CommandServices;
using NovaTech.TerraTech.Platform.Iam.Interface.Rest.Resources;

namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest;

[ApiController, Route("api/v1/authentication"), AllowAnonymous]
public class AuthenticationController(AccountService accounts) : ControllerBase
{
    [EnableRateLimiting("account-email")]
    [HttpPost("sign-up"), ProducesResponseType<AuthenticatedUserResource>(201)]
    public async Task<IActionResult> SignUp(SignUpResource resource, CancellationToken ct)
    {
        var user = await accounts.Register(resource.FullName, resource.EmailAddress, resource.Password, resource.ConfirmPassword, ct);
        return Created($"/api/v1/users/{user.Id}", new AuthenticatedUserResource(user.Id, user.EmailAddress.Value, null, user.FullName, null, true));
    }

    [HttpPost("sign-in"), ProducesResponseType<AuthenticatedUserResource>(200), ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> SignIn(SignInResource resource, CancellationToken ct)
    {
        var (user, token) = await accounts.Login(resource.EmailAddress, resource.Password, ct);
        return Ok(new AuthenticatedUserResource(user.Id, user.EmailAddress.Value, token, user.FullName, new JsonWebToken(token).ValidTo));
    }
}
