using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;
using NovaTech.TerraTech.Platform.Shared.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST;

[ApiController]
[Route("api/v1/fields/{fieldId:int}/fertility-zones")]
[Tags("Monitoring")]
public class FertilityZonesController(
    IFertilityZoneQueryService fertilityZoneQueryService,
    IFertilityZoneCommandService fertilityZoneCommandService,
    IFieldQueryService fieldQueryService,
    IStringLocalizer<CommonMessages> localizer,
    ILogger<FertilityZonesController> logger
) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Gets fertility zones by field id",
        Description = "Retrieves geographic fertility zones associated with a field",
        OperationId = "GetFertilityZonesByFieldId"
    )]
    [SwaggerResponse(
        200,
        "List of fertility zones",
        typeof(IEnumerable<FertilityZoneResource>)
    )]
    [SwaggerResponse(404, "Field not found")]
    [SwaggerResponse(500, "Unexpected server error")]
    public async Task<ActionResult<IEnumerable<FertilityZoneResource>>>
        GetZonesByFieldId(
            [FromRoute] int fieldId,
            CancellationToken cancellationToken)
    {
        try
        {
            var field = await fieldQueryService.GetFieldByIdAsync(
                fieldId,
                cancellationToken
            );

            if (field is null)
                return NotFound();

            var zones = await fertilityZoneQueryService
                .GetZonesByFieldIdAsync(
                    fieldId,
                    cancellationToken
                );

            var resources = zones
                .Select(
                    FertilityZoneResourceFromEntityAssembler
                        .ToResourceFromEntity
                )
                .ToList();

            return Ok(resources);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error retrieving fertility zones for field {FieldId}",
                fieldId
            );

            return Problem(
                title: localizer["UnexpectedServerError"].Value,
                detail: localizer["UnexpectedErrorProcessingRequest"].Value,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Creates a fertility zone",
        Description = "Creates a geographic fertility zone associated with a field and sensor",
        OperationId = "CreateFertilityZone"
    )]
    [SwaggerResponse(
        201,
        "The fertility zone was created",
        typeof(FertilityZoneResource)
    )]
    [SwaggerResponse(400, "Invalid fertility zone data")]
    [SwaggerResponse(404, "Field or sensor not found")]
    [SwaggerResponse(500, "Unexpected server error")]
    public async Task<ActionResult<FertilityZoneResource>>
        CreateFertilityZone(
            [FromRoute] int fieldId,
            [FromBody] CreateFertilityZoneResource resource,
            CancellationToken cancellationToken)
    {
        try
        {
            var boundary = resource.Boundary
                .Select(vertex => new FieldVertex(
                    vertex.Latitude,
                    vertex.Longitude
                ))
                .ToList();

            var zone = await fertilityZoneCommandService
                .CreateZoneAsync(
                    fieldId,
                    resource.DeviceId,
                    resource.Name,
                    boundary,
                    cancellationToken
                );

            var response =
                FertilityZoneResourceFromEntityAssembler
                    .ToResourceFromEntity(zone);

            return StatusCode(
                StatusCodes.Status201Created,
                response
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(
                ex,
                "Invalid fertility zone data for field {FieldId}",
                fieldId
            );

            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error creating fertility zone for field {FieldId}",
                fieldId
            );

            return Problem(
                title: localizer["UnexpectedServerError"].Value,
                detail: localizer["UnexpectedErrorProcessingRequest"].Value,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}

