
using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST;

[ApiController]
[Route("api/v1/fields/{fieldId:int}/fertility-zones/{zoneId:int}/thresholds")]
[Tags("Monitoring")]
public class FertilityZoneThresholdsController(
    IFertilityZoneThresholdQueryService queryService,
    IFertilityZoneThresholdCommandService commandService
) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Gets the alert thresholds of a fertility zone")]
    [SwaggerResponse(200, "Thresholds found", typeof(FertilityZoneThresholdsResource))]
    [SwaggerResponse(404, "Fertility zone not found")]
    public async Task<ActionResult<FertilityZoneThresholdsResource>> GetThresholds(
        [FromRoute] int fieldId,
        [FromRoute] int zoneId,
        CancellationToken cancellationToken)
    {
        try
        {
            var zone = await queryService.GetThresholdsAsync(
                fieldId,
                zoneId,
                cancellationToken
            );

            return Ok(
                FertilityZoneThresholdsResourceFromEntityAssembler
                    .ToResourceFromEntity(zone)
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Updates the alert thresholds of a fertility zone")]
    [SwaggerResponse(200, "Thresholds updated", typeof(FertilityZoneThresholdsResource))]
    [SwaggerResponse(400, "Invalid threshold range")]
    [SwaggerResponse(404, "Fertility zone not found")]
    public async Task<ActionResult<FertilityZoneThresholdsResource>> UpdateThresholds(
        [FromRoute] int fieldId,
        [FromRoute] int zoneId,
        [FromBody] UpdateFertilityZoneThresholdsResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var zone = await commandService.UpdateThresholdsAsync(
                fieldId,
                zoneId,
                resource.MoistureMin!.Value,
                resource.MoistureMax!.Value,
                resource.NitrogenMin!.Value,
                resource.NitrogenMax!.Value,
                resource.PhosphorusMin!.Value,
                resource.PhosphorusMax!.Value,
                resource.PotassiumMin!.Value,
                resource.PotassiumMax!.Value,
                cancellationToken
            );

            return Ok(
                FertilityZoneThresholdsResourceFromEntityAssembler
                    .ToResourceFromEntity(zone)
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete]
    [SwaggerOperation(Summary = "Resets the alert thresholds of a fertility zone")]
    [SwaggerResponse(204, "Thresholds reset")]
    [SwaggerResponse(404, "Fertility zone not found")]
    public async Task<IActionResult> ResetThresholds(
        [FromRoute] int fieldId,
        [FromRoute] int zoneId,
        CancellationToken cancellationToken)
    {
        try
        {
            await commandService.ResetThresholdsAsync(
                fieldId,
                zoneId,
                cancellationToken
            );

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
