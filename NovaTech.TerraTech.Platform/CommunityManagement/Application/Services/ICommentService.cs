using NovaTech.TerraTech.Platform.CommunityManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Shared.Application.Model;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Application.Services;

public interface ICommentService
{
    Task<Result<Comment>> Handle(CreateCommentCommand command, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> Handle(GetCommentsByTargetProfileIdQuery query, CancellationToken cancellationToken = default);
    Task<Comment?> Handle(GetCommentByIdQuery query, CancellationToken cancellationToken = default);
    Task<Result<Comment>> Handle(UpdateCommentCommand command, CancellationToken cancellationToken = default);
    Task<bool> Handle(DeleteCommentCommand command, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Comment>>> Handle(GetCommentsByProductIdQuery query, CancellationToken cancellationToken = default);
    Task<Result<ProductRatingSummary>> Handle(GetProductRatingSummaryQuery query, CancellationToken cancellationToken = default);
}
