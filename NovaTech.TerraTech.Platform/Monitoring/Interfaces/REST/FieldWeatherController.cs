using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST;

[ApiController]
[Route("api/v1/fields/{fieldId:int}/weather")]
[Tags("Monitoring")]
public class FieldWeatherController(
    IWeatherForecastService weatherForecastService,
    ILogger<FieldWeatherController> logger) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Gets the weather forecast for a field",
        Description = "Retrieves the current weather forecast, rainfall probability and irrigation recommendation based on the field location and soil conditions",
        OperationId = "GetFieldWeatherForecast")]
    [SwaggerResponse(200, "The weather forecast was successfully retrieved", typeof(FieldWeatherResource))]
    [SwaggerResponse(404, "The field was not found")]
    [SwaggerResponse(502, "The external weather service is unavailable")]
    public async Task<ActionResult<FieldWeatherResource>> GetWeatherByFieldId(int fieldId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await weatherForecastService.GetWeatherByFieldIdAsync(fieldId, cancellationToken);
            if (result is null)
                return NotFound(new ProblemDetails { Status = 404, Title = "Field was not found.", Extensions = { ["code"] = "FIELD_NOT_FOUND" } });

            var resource = FieldWeatherResourceFromEntityAssembler.ToResourceFromEntity(result);
            return Ok(resource);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Weather provider error for field {FieldId}", fieldId);
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Status = 502,
                Title = "The weather provider is unavailable.",
                Extensions = { ["code"] = "WEATHER_SERVICE_UNAVAILABLE" }
            });
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Weather provider timeout for field {FieldId}", fieldId);
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Status = 502,
                Title = "The weather provider timed out.",
                Extensions = { ["code"] = "WEATHER_SERVICE_TIMEOUT" }
            });
        }
    }
}
