using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.Monitoring.Interfaces.REST.Transform;

public static class FertilityZoneResourceFromEntityAssembler
{
    public static FertilityZoneResource ToResourceFromEntity(
        FertilityZone entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Boundary is null)
            throw new InvalidOperationException(
                "Fertility zone boundary is missing."
            );

        var boundary = entity.Boundary
            .Select(vertex => new FieldVertexResource(
                vertex.Latitude,
                vertex.Longitude
            ))
            .ToList();

        return new FertilityZoneResource(
            entity.Id,
            entity.FieldId,
            entity.DeviceId,
            entity.Name,
            boundary
        );
    }
}
