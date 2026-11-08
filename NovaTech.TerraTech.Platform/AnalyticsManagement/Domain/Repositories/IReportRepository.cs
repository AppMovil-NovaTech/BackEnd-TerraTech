using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Repositories;
public interface IReportRepository : IBaseRepository<Report>
{
    Task<IEnumerable<Report>> FindByDeviceIdAsync(DeviceId deviceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Report>> FindByDateRangeAsync(GeneratedAt from, GeneratedAt to, CancellationToken cancellationToken = default);
    Task<Report?> FindByDeviceIdAndGeneratedAtAsync(DeviceId deviceId, GeneratedAt generatedAt, CancellationToken cancellationToken = default);
    Task<Report?> FindByFieldIdAsync(int fieldId, CancellationToken cancellationToken = default);
}
