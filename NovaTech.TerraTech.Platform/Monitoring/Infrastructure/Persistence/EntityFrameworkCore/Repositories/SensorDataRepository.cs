using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
public class SensorDataRepository(AppDbContext db) : ISensorDataRepository
{
    public Task<SensorCatalogItem?> FindSensor(string? code, string? mac, CancellationToken ct) => db.Set<SensorCatalogItem>().SingleOrDefaultAsync(x => code != null ? x.SensorCode == code : x.MacAddress.Replace("-", ":").ToUpper() == mac, ct);
    public Task<bool> IsAssociated(string code, CancellationToken ct) => db.Set<Device>().IgnoreQueryFilters().AnyAsync(x => x.SensorCode == code, ct);
    public Task<SensorReading?> FindReading(int deviceId, int readingId, CancellationToken ct) => db.Set<SensorReading>().SingleOrDefaultAsync(x => x.DeviceId == deviceId && x.Id == readingId, ct);
    public Task<SensorReading?> Latest(int deviceId, CancellationToken ct) => db.Set<SensorReading>().Where(x => x.DeviceId == deviceId).OrderByDescending(x => x.RecordedAt).FirstOrDefaultAsync(ct);
    public Task<List<SensorReading>> History(int deviceId, DateTime from, DateTime to, CancellationToken ct) => db.Set<SensorReading>().Where(x => x.DeviceId == deviceId && x.RecordedAt >= from && x.RecordedAt <= to).OrderBy(x => x.RecordedAt).ToListAsync(ct);
    public async Task AddReadingAsync(
        SensorReading reading,
        CancellationToken cancellationToken)
    {
        await db.Set<SensorReading>().AddAsync(
            reading,
            cancellationToken
        );
    }
}
