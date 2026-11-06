using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Shared.Application.Model;

namespace NovaTech.TerraTech.Platform.AnalyticsManagement.Application.Services;
public interface IReportCommandService
{
    Task<Result<Report>> Handle(CreateReportCommand command, CancellationToken cancellationToken = default);
    Task<Result<Report>> Handle(UpdateReportCommand command, CancellationToken cancellationToken = default);
    Task<Report?> GenerateFieldProjectionAsync(int fieldId, CancellationToken cancellationToken = default);
}
