using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
public class ReportRepository(AppDbContext context) : BaseRepository<Report>(context), IReportRepository
{
    public async Task<Report?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Report>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Report>> FindByDeviceIdAsync(DeviceId deviceId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Report>().Where(r => r.DeviceId.Value == deviceId.Value).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Report>> FindByDateRangeAsync(GeneratedAt from, GeneratedAt to, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Report>().Where(r => r.GeneratedAt.Value >= from.Value && r.GeneratedAt.Value <= to.Value).ToListAsync(cancellationToken);
    }

    public async Task<Report?> FindByDeviceIdAndGeneratedAtAsync(DeviceId deviceId, GeneratedAt generatedAt, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Report>().FirstOrDefaultAsync(r => r.DeviceId.Value == deviceId.Value && r.GeneratedAt.Value == generatedAt.Value, cancellationToken);
    }

    public async Task<Report?> FindByFieldIdAsync(int fieldId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Report>().OrderByDescending(r => r.Id).FirstOrDefaultAsync(r => r.FieldId == fieldId, cancellationToken);
    }
}
