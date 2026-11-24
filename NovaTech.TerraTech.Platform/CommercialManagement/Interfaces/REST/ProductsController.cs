using NovaTech.TerraTech.Platform.CommercialManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Shared.Tb1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST;
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Tags("Commercials")]
public class ProductsController(IProductService productService, ILogger<ProductsController> logger) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Creates a new product", Description = "Creates a product for the catalog")]
    [SwaggerResponse(201, "Product created", typeof(ProductResource))]
    [SwaggerResponse(400, "Invalid request", typeof(string))]
    [SwaggerResponse(500, "Unexpected error", typeof(ProblemDetails))]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = CreateProductCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await productService.Handle(command, cancellationToken);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetProductById), new { id = result.Value.Id }, ProductResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
            }

            return BadRequest("Invalid product request");
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product");
            return Problem(title: "Unexpected server error", detail: "An unexpected error occurred while processing your request", statusCode: 500);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    [SwaggerOperation(Summary = "Gets all products")]
    [SwaggerResponse(200, "Products retrieved", typeof(IEnumerable<ProductResource>))]
    public async Task<IActionResult> GetAllProducts(CancellationToken cancellationToken)
    {
        var query = new GetAllProductsQuery();
        var products = await productService.Handle(query, cancellationToken);
        var resources = products.Select(ProductResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(resources);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Gets a product by id")]
    [SwaggerResponse(200, "Product found", typeof(ProductResource))]
    [SwaggerResponse(404, "Product not found")]
    public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var product = await productService.Handle(query, cancellationToken);
        if (product == null)
            return NotFound();
        var resource = ProductResourceFromEntityAssembler.ToResourceFromEntity(product);
        return Ok(resource);
    }

    [AllowAnonymous]
    [HttpPost("{id:int}/consultations")]
    [SwaggerOperation(Summary = "Records a consultation of a product detail")]
    [SwaggerResponse(204, "Consultation registered")]
    [SwaggerResponse(404, "Product not found")]
    public async Task<IActionResult> RegisterProductConsultation(int id, CancellationToken cancellationToken)
    {
        var result = await productService.Handle(new RegisterProductConsultationCommand(id), cancellationToken);
        if (result.IsSuccess)
            return NoContent();
        if (result.Error is CommercialError.NotFound)
            return NotFound();
        if (result.Error is CommercialError.InvalidProductId)
            return BadRequest();
        return Problem(title: "Could not register consultation", statusCode: 500);
    }

    [Authorize]
    [HttpPut("{id:int}/interest-threshold")]
    [SwaggerOperation(Summary = "Configures the interest threshold of an owned product")]
    [SwaggerResponse(200, "Threshold updated", typeof(ProductInterestResource))]
    [SwaggerResponse(404, "Product not found for the current user")]
    public async Task<IActionResult> ConfigureProductInterestThreshold(
        int id, [FromBody] ConfigureProductInterestThresholdResource resource, CancellationToken cancellationToken)
    {
        var command = ConfigureProductInterestThresholdCommandFromResourceAssembler.ToCommandFromResource(
            id, User.UserId(), resource);
        var result = await productService.Handle(command, cancellationToken);
        if (result.IsSuccess && result.Value != null)
            return Ok(ProductInterestResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
        if (result.Error is CommercialError.NotFound)
            return NotFound();
        if (result.Error is CommercialError.InvalidProductId)
            return BadRequest();
        return Problem(title: "Could not configure interest threshold", statusCode: 500);
    }

    [Authorize]
    [HttpGet("{id:int}/interest")]
    [SwaggerOperation(Summary = "Gets interest statistics for an owned product")]
    [SwaggerResponse(200, "Product interest", typeof(ProductInterestResource))]
    [SwaggerResponse(404, "Product not found for the current user")]
    public async Task<IActionResult> GetProductInterest(int id, CancellationToken cancellationToken)
    {
        var result = await productService.Handle(new GetProductInterestQuery(id, User.UserId()), cancellationToken);
        if (result.IsSuccess && result.Value != null)
            return Ok(ProductInterestResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
        if (result.Error is CommercialError.NotFound)
            return NotFound();
        return Problem(title: "Could not load product interest", statusCode: 500);
    }

    [Authorize]
    [HttpPost("{id:int}/check-inactivity")]
    [SwaggerOperation(Summary = "Checks 30-day inactivity for an owned product")]
    [SwaggerResponse(204, "Inactivity evaluated")]
    [SwaggerResponse(404, "Product not found for the current user")]
    public async Task<IActionResult> CheckProductInactivity(int id, CancellationToken cancellationToken)
    {
        var result = await productService.Handle(new CheckProductInactivityCommand(id, User.UserId()), cancellationToken);
        if (result.IsSuccess)
            return NoContent();
        if (result.Error is CommercialError.NotFound)
            return NotFound();
        return Problem(title: "Could not check product inactivity", statusCode: 500);
    }
    [Authorize]
    [HttpPut("{id:int}/price")]
    [SwaggerOperation(Summary = "Updates a product's published price and notifies its followers (US32)")]
    public async Task<IActionResult> UpdatePublishedPrice(int id, [FromBody] UpdateProductPriceResource resource, CancellationToken ct)
    {
        if (resource is null || resource.Price <= 0m) return BadRequest();
        var result = await productService.Handle(new UpdateProductPriceCommand(id, User.UserId(), resource.Price), ct);
        if (result.IsSuccess && result.Value is not null)
            return Ok(ProductResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
        if (result.Error is CommercialError.NotFound) return NotFound();
        if (result.Error is CommercialError.InvalidProductId) return BadRequest();
        return Problem(title: "Could not update published price", statusCode: 500);
    }
}
