using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
using NovaTech.TerraTech.Platform.Shared.Tb1;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST;

[ApiController]
[Authorize]
[Route("api/v1/products/{productId:int}/favorite")]
[Produces("application/json")]
[Tags("Commercials")]
public class ProductFavoritesController(IFavoriteProductService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Gets whether the current user has favorited a product (US32)")]
    public async Task<IActionResult> Get(int productId, CancellationToken ct)
    {
        var userId = User.UserId();
        if (userId <= 0) return Unauthorized();
        if (productId <= 0) return BadRequest();
        return Ok(new { isFavorite = await service.IsFavoriteAsync(userId, productId, ct) });
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Adds a product to the current user's favorites (US32)")]
    public async Task<IActionResult> Add(int productId, CancellationToken ct)
    {
        var userId = User.UserId();
        if (userId <= 0) return Unauthorized();
        try
        {
            await service.AddAsync(userId, productId, ct);
            return NoContent();
        }
        catch (ArgumentException) { return BadRequest(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete]
    [SwaggerOperation(Summary = "Removes a product from the current user's favorites (US32)")]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        var userId = User.UserId();
        if (userId <= 0) return Unauthorized();
        if (productId <= 0) return BadRequest();
        await service.RemoveAsync(userId, productId, ct);
        return NoContent();
    }
}
