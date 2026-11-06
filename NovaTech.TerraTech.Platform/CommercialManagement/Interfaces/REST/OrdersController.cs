using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST.Transform;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Interfaces.REST;
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Tags("Commercials")]
public class OrdersController(IOrderService orderService, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Creates a new order", Description = "Creates an order for a product")]
    [SwaggerResponse(201, "Order created", typeof(OrderResource))]
    [SwaggerResponse(400, "Invalid request", typeof(string))]
    [SwaggerResponse(409, "Insufficient stock", typeof(ProblemDetails))]
    [SwaggerResponse(500, "Unexpected error", typeof(ProblemDetails))]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = CreateOrderCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await orderService.Handle(command, cancellationToken);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetOrderById), new { id = result.Value.Id }, OrderResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
            }

            return (CommercialError)result.Error switch
            {
                CommercialError.InsufficientStock => Conflict("Insufficient stock available for the requested product"),
                CommercialError.StockConflict => Conflict("Stock changed concurrently; please retry the order"),
                CommercialError.InvalidProductId or CommercialError.InvalidOrderStatus => BadRequest("Invalid order request"),
                _ => Problem(title: "Unexpected server error", detail: "An unexpected error occurred while processing your request", statusCode: 500)};
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order");
            return Problem(title: "Unexpected server error", detail: "An unexpected error occurred while processing your request", statusCode: 500);
        }
    }

    [HttpPut("{id}/validates")]
    [SwaggerOperation(Summary = "Validates an order", Description = "Validates an order before payment")]
    [SwaggerResponse(200, "Order validated", typeof(OrderResource))]
    [SwaggerResponse(404, "Order not found", typeof(string))]
    [SwaggerResponse(500, "Unexpected error", typeof(ProblemDetails))]
    public async Task<IActionResult> ValidateOrder(int id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ValidateOrderCommand(id);
            var result = await orderService.Handle(command, cancellationToken);
            if (result.IsSuccess)
            {
                return Ok(OrderResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
            }

            return (CommercialError)result.Error == CommercialError.NotFound ? NotFound() : Problem(title: "Unexpected server error", statusCode: 500);
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating order {Id}", id);
            return Problem(title: "Unexpected server error", statusCode: 500);
        }
    }

    [HttpPut("{id}/status")]
    [SwaggerOperation(Summary = "Updates order status", Description = "Updates the status of an order")]
    [SwaggerResponse(200, "Order status updated", typeof(OrderResource))]
    [SwaggerResponse(400, "Invalid status transition", typeof(string))]
    [SwaggerResponse(404, "Order not found", typeof(string))]
    [SwaggerResponse(500, "Unexpected error", typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateOrderStatusCommand(id, resource.Status);
            var result = await orderService.Handle(command, cancellationToken);
            if (result.IsSuccess)
            {
                return Ok(OrderResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
            }

            return (CommercialError)result.Error switch
            {
                CommercialError.NotFound => NotFound(),
                CommercialError.InvalidOrderStatus => BadRequest("Invalid order status transition"),
                CommercialError.StockConflict => Conflict("Stock changed concurrently; please retry the status update"),
                _ => Problem(title: "Unexpected server error", statusCode: 500)
            };
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating order status {Id}", id);
            return Problem(title: "Unexpected server error", statusCode: 500);
        }
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Gets all orders")]
    [SwaggerResponse(200, "Orders retrieved", typeof(IEnumerable<OrderResource>))]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();
        var orders = await orderService.Handle(query, cancellationToken);
        var resources = orders.Select(OrderResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(resources);
    }

    [HttpGet("~/api/v1/profiles/{profileId}/orders")]
    [SwaggerOperation(Summary = "Gets orders by profile")]
    [SwaggerResponse(200, "Orders retrieved", typeof(IEnumerable<OrderResource>))]
    public async Task<IActionResult> GetOrdersByProfile([FromRoute] int profileId, CancellationToken cancellationToken)
    {
        var query = new GetOrdersByProfileQuery(profileId);
        var orders = await orderService.Handle(query, cancellationToken);
        var resources = orders.Select(OrderResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(resources);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Gets an order by id")]
    [SwaggerResponse(200, "Order found", typeof(OrderResource))]
    [SwaggerResponse(404, "Order not found")]
    public async Task<IActionResult> GetOrderById(int id, CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var order = await orderService.Handle(query, cancellationToken);
        if (order == null)
            return NotFound();
        var resource = OrderResourceFromEntityAssembler.ToResourceFromEntity(order);
        return Ok(resource);
    }
}
