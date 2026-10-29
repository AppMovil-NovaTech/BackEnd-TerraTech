using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST;

[ApiController]
[Route("api/v1/fields/{fieldId:int}/satellite-images")]
[Tags("Monitoring")]
public class SatelliteImagesController(
    ISatelliteImageService satelliteImageService) : ControllerBase
{
    [HttpGet("latest")]
    [SwaggerOperation(
        Summary = "Gets the latest satellite image for a field",
        Description = "Retrieves the most recent satellite image URL, vegetation index (NDVI), cloud coverage and bounding coordinates for a field",
        OperationId = "GetLatestSatelliteImage")]
    [SwaggerResponse(200, "The latest satellite image was successfully retrieved", typeof(SatelliteImageResource))]
    [SwaggerResponse(404, "No recent satellite image was found for the field")]
    public async Task<ActionResult<SatelliteImageResource>> GetLatestImage(int fieldId, CancellationToken cancellationToken)
    {
        var image = await satelliteImageService.GetLatestImageAsync(fieldId, cancellationToken);
        if (image is null)
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Satellite image not found.",
                Extensions = { ["code"] = "SATELLITE_IMAGE_NOT_FOUND" }
            });

        return Ok(SatelliteImageResourceFromEntityAssembler.ToResourceFromEntity(image));
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Gets satellite image for a field by target date",
        Description = "Retrieves the satellite image closest to the specified target date, including cloud coverage and capture date",
        OperationId = "GetSatelliteImageByDate")]
    [SwaggerResponse(200, "The satellite image was successfully retrieved", typeof(SatelliteImageResource))]
    [SwaggerResponse(404, "No satellite image was found for the specified date")]
    public async Task<ActionResult<SatelliteImageResource>> GetImageByDate(int fieldId, [FromQuery] DateTime? targetDate, CancellationToken cancellationToken)
    {
        var image = targetDate.HasValue
            ? await satelliteImageService.GetImageByTargetDateAsync(fieldId, targetDate.Value, cancellationToken)
            : await satelliteImageService.GetLatestImageAsync(fieldId, cancellationToken);

        if (image is null)
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Satellite image not found for the requested date.",
                Extensions = { ["code"] = "SATELLITE_IMAGE_NOT_FOUND" }
            });

        return Ok(SatelliteImageResourceFromEntityAssembler.ToResourceFromEntity(image));
    }
}
