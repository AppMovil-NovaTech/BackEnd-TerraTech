
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NovaTech.TerraTech.Platform.Monitoring.Application.Services;
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
}
