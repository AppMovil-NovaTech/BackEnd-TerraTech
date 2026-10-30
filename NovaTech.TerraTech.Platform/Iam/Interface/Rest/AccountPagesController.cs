using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest;

[ApiExplorerSettings(IgnoreApi = true), AllowAnonymous, Route("account")]
public class AccountPagesController(IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("verify-email")]
    public IActionResult Verification() => Page("verify-email.html");
    [HttpGet("reset-password")]
    public IActionResult PasswordReset() => Page("reset-password.html");
    private IActionResult Page(string file)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'";
        return PhysicalFile(Path.Combine(environment.ContentRootPath, "wwwroot", "account", file), "text/html; charset=utf-8");
    }
}
