using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.Shared.Tb1;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST;

[ApiController]
[Authorize]
[Route("api/v1/product-preferences")]
[Produces("application/json")]
[Tags("Commercials")]
public sealed class ProductPreferencesController(IProductPreferenceService preferences) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        Ok(await preferences.GetMineAsync(User.UserId(), ct));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct) =>
        Ok(await preferences.GetAvailableCategoriesAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] CreateProductPreferenceResource resource, CancellationToken ct)
    {
        if (resource is null || string.IsNullOrWhiteSpace(resource.Category)) return BadRequest();
        try
        {
            await preferences.AddAsync(User.UserId(), resource.Category, ct);
            return NoContent();
        }
        catch (ArgumentException) { return BadRequest("Invalid product category."); }
        catch (InvalidOperationException) { return BadRequest("A customer profile is required."); }
    }

    [HttpDelete]
    public async Task<IActionResult> Unsubscribe([FromQuery] string category, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category)) return BadRequest();
        try
        {
            await preferences.RemoveAsync(User.UserId(), category, ct);
            return NoContent();
        }
        catch (ArgumentException) { return BadRequest("Invalid product category."); }
    }
}
