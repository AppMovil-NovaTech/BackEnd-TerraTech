using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Monitoring.Application.Services;

public interface IFertilityZoneCommandService
{
    Task<FertilityZone> CreateZoneAsync(
        int fieldId,
        int deviceId,
        string name,
        IReadOnlyList<FieldVertex> boundary,
        CancellationToken cancellationToken = default
    );
}
