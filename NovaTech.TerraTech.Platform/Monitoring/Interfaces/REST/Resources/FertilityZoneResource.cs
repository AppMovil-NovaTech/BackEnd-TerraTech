using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

[SwaggerSchema(Description = "A geographic fertility zone resource")]
public record FertilityZoneResource(
    int Id,
    int FieldId,
    int DeviceId,
    string Name,
    IReadOnlyList<FieldVertexResource> Boundary
);
