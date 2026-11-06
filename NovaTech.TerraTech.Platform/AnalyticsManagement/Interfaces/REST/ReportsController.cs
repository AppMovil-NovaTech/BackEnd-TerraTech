using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Services;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Interfaces.REST.Transform;
using NovaTech.TerraTech.Platform.Shared.Resources;
using Swashbuckle.AspNetCore.Annotations;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Errors;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Interfaces.REST;
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Analytics")]
public class ReportsController(IReportCommandService reportCommandService, IReportQueryService reportQueryService, IStringLocalizer<CommonMessages> localizer, ILogger<ReportsController> logger) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Creates a Report", Description = "Creates a new statistical report for a device", OperationId = "CreateReport")]
    [SwaggerResponse(201, "The report was created", typeof(ReportResource))]
    [SwaggerResponse(400, "The request payload is invalid", typeof(string))]
    [SwaggerResponse(409, "The report already exists for that device and date", typeof(string))]
    [SwaggerResponse(500, "Unexpected server error", typeof(ProblemDetails))]
    public async Task<ActionResult> CreateReport([FromBody] CreateReportResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = CreateReportCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await reportCommandService.Handle(command, cancellationToken);
            return ActionResultFromCreateReportResultAssembler.ToActionResultFromCreateReportResult(result, this, localizer, nameof(GetReportById));
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while creating report for DeviceId {DeviceId}", resource.DeviceId);
            return BadRequest(localizer["InvalidReportRequest"].Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating report for DeviceId {DeviceId}", resource.DeviceId);
            return Problem(title: localizer["UnexpectedServerError"].Value, detail: localizer["UnexpectedErrorCreatingReport"].Value, statusCode: 500);
        }
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Updates an existing report", Description = "Updates the statistical values and interpretation of a report", OperationId = "UpdateReport")]
    [SwaggerResponse(200, "The report was updated", typeof(ReportResource))]
    [SwaggerResponse(400, "Invalid request data", typeof(string))]
    [SwaggerResponse(404, "Report not found", typeof(string))]
    [SwaggerResponse(500, "Unexpected server error", typeof(ProblemDetails))]
    public async Task<ActionResult> UpdateReport([FromRoute] int id, [FromBody] UpdateReportResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateReportCommand(id, resource.MeanValue, resource.Variance, resource.StandardDeviation, resource.TechnicalInterpretation);
            var result = await reportCommandService.Handle(command, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error switch
                {
                    UpdateReportError.ReportNotFound => NotFound(new { error = localizer["ReportNotFound"].Value }),
                    UpdateReportError.InvalidData => BadRequest(new { error = result.Message }),
                    _ => Problem(title: localizer["UnexpectedServerError"].Value, detail: result.Message, statusCode: StatusCodes.Status500InternalServerError)};
            }

            var resourceResponse = ReportResourceFromEntityAssembler.ToResourceFromEntity(result.Value);
            return Ok(resourceResponse);
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed while updating report with id {ReportId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while updating report with id {ReportId}", id);
            return Problem(title: localizer["UnexpectedServerError"].Value, detail: localizer["UnexpectedErrorUpdatingReport"].Value, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Gets all reports", Description = "Retrieves all available reports", OperationId = "GetAllReports")]
    [SwaggerResponse(200, "List of all reports", typeof(IEnumerable<ReportResource>))]
    public async Task<ActionResult<IEnumerable<ReportResource>>> GetAllReports(CancellationToken cancellationToken)
    {
        try
        {
            var reports = await reportQueryService.GetAllReportsAsync(cancellationToken);
            var resources = reports.Select(ReportResourceFromEntityAssembler.ToResourceFromEntity);
            return Ok(resources);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving all reports");
            return Problem(title: localizer["UnexpectedServerError"].Value, detail: localizer["UnexpectedErrorProcessingRequest"].Value, statusCode: 500);
        }
    }

    [HttpGet("fields/{fieldId:int}")]
    [SwaggerOperation(Summary = "Gets or generates a yield projection report for a field", Description = "Calculates and persists a yield projection report for the specified field", OperationId = "GetFieldProductionReport")]
    [SwaggerResponse(200, "The field projection report was generated", typeof(ProductionReportResource))]
    [SwaggerResponse(404, "The field was not found")]
    public async Task<ActionResult<ProductionReportResource>> GetFieldProductionReport([FromRoute] int fieldId, CancellationToken cancellationToken)
    {
        try
        {
            var report = await reportCommandService.GenerateFieldProjectionAsync(fieldId, cancellationToken);
            if (report is null)
                return NotFound();
            var resource = ProductionReportResourceFromEntityAssembler.ToResourceFromEntity(report);
            return Ok(resource);
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while generating projection report for FieldId {FieldId}", fieldId);
            return Problem(title: localizer["UnexpectedServerError"].Value, detail: localizer["UnexpectedErrorProcessingRequest"].Value, statusCode: 500);
        }
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Gets a report by id", Description = "Retrieves a specific report by its identifier", OperationId = "GetReportById")]
    [SwaggerResponse(200, "The report was found", typeof(ReportResource))]
    [SwaggerResponse(404, "The report was not found")]
    public async Task<ActionResult> GetReportById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var report = await reportQueryService.GetReportByIdAsync(id, cancellationToken);
            if (report is null)
                return NotFound();
            var resource = ReportResourceFromEntityAssembler.ToResourceFromEntity(report);
            return Ok(resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving report with id {ReportId}", id);
            return Problem(title: localizer["UnexpectedServerError"].Value, detail: localizer["UnexpectedErrorProcessingRequest"].Value, statusCode: 500);
        }
    }
}
