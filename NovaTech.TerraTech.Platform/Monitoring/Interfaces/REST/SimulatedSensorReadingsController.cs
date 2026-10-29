
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.Monitoring.Application.Internal.CommandServices;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST;

[ApiController]
[Authorize]
[Route("api/v1/devices/{deviceId:int}/readings")]
[Produces("application/json")]
[Tags("Monitoring")]
public class SimulatedSensorReadingsController(
    SensorReadingCommandService commands,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("simulate")]
    [SwaggerOperation(
        Summary = "Simulate a sensor reading",
        Description = "Development only. Saves a simulated reading and generates fertility threshold alerts."
    )]
    [SwaggerResponse(201, "Simulated reading created")]
    [SwaggerResponse(400, "Invalid reading")]
    [SwaggerResponse(404, "Device not found")]
    public async Task<IActionResult> Simulate(
        [FromRoute] int deviceId,
        [FromBody] SimulateSensorReadingResource resource,
        CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var reading = await commands.Register(
            deviceId,
            resource.MoisturePercent!.Value,
            resource.SoilTemperatureC!.Value,
            resource.NitrogenPpm!.Value,
            resource.PhosphorusPpm!.Value,
            resource.PotassiumPpm!.Value,
            ct
        );

        var response = new ReadingResource(
            reading.Id,
            reading.DeviceId,
            DateTime.SpecifyKind(
                reading.RecordedAt,
                DateTimeKind.Utc
            ),
            reading.MoisturePercent,
            reading.SoilTemperatureC,
            reading.NitrogenPpm,
            reading.PhosphorusPpm,
            reading.PotassiumPpm,
            reading.Source
        );

        return Created(
            $"/api/v1/devices/{deviceId}/readings/{reading.Id}",
            response
        );
    }
}

