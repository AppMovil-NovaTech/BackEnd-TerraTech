using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest;

[ApiExplorerSettings(IgnoreApi = true), AllowAnonymous, Route("account")]
public class AccountPagesController(IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("verify-email")]
    public IActionResult Verification() => Asset("verify-email.html", "text/html; charset=utf-8");
    [HttpGet("reset-password")]
    public IActionResult PasswordReset() => Asset("reset-password.html", "text/html; charset=utf-8");
    [HttpGet("account.css")]
    public IActionResult Styles() => Asset("account.css", "text/css; charset=utf-8");
    [HttpGet("account.js")]
    public IActionResult Script() => Asset("account.js", "application/javascript; charset=utf-8");
    private IActionResult Asset(string file, string contentType)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'";
        return PhysicalFile(Path.Combine(environment.ContentRootPath, "wwwroot", "account", file), contentType);
    }
}
