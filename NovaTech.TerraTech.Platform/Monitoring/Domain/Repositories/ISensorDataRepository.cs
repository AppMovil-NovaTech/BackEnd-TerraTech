using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
public interface ISensorDataRepository
{
    Task<SensorCatalogItem?> FindSensor(string? code, string? mac, CancellationToken ct);
    Task<bool> IsAssociated(string code, CancellationToken ct);
    Task<SensorReading?> FindReading(int deviceId, int readingId, CancellationToken ct);
    Task<SensorReading?> Latest(int deviceId, CancellationToken ct);
    Task<List<SensorReading>> History(int deviceId, DateTime from, DateTime to, CancellationToken ct);
    Task AddReadingAsync(
        SensorReading reading,
        CancellationToken cancellationToken);

}
