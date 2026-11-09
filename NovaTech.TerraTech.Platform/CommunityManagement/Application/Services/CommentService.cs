using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.CommunityManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Application.Services;

public class CommentService(
    ICommentRepository commentRepository,
    IProductRepository productRepository,
    IInventoryRepository inventoryRepository,
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork,
    ILogger<CommentService> logger) : ICommentService
{
    public async Task<Result<Comment>> Handle(CreateCommentCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (command.Rating < 1 || command.Rating > 5)
                return Result<Comment>.Failure(CommunityError.InvalidComment, "Rating must be strictly between 1 and 5");

            var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
                return Result<Comment>.Failure(CommunityError.InvalidComment, "The specified product was not found");

            var inventory = await inventoryRepository.FindByProductIdAsync(command.ProductId, cancellationToken);
            if (inventory != null)
            {
                if (inventory.OwnerUserId == command.UserId)
                    return Result<Comment>.Failure(CommunityError.SelfReviewNotAllowed, "Producers cannot review their own products");
                if (inventory.ProfileId != command.TargetProfileId)
                    return Result<Comment>.Failure(CommunityError.InvalidComment, "The target profile does not own the reviewed product");
            }

            // The profile query filter only returns profiles owned by the caller, so a hit here means self-review.
            var callerOwnedProfile = await profileRepository.FindByIdAsync(command.TargetProfileId, cancellationToken);
            if (callerOwnedProfile != null)
                return Result<Comment>.Failure(CommunityError.SelfReviewNotAllowed, "Producers cannot review their own products");

            var comment = new Comment(command);
            await commentRepository.AddAsync(comment, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Comment>.Success(comment);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while creating comment for ProfileId {TargetProfileId}", command.TargetProfileId);
            return Result<Comment>.Failure(CommunityError.InvalidComment, "The provided comment data is invalid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating comment for ProfileId {TargetProfileId}", command.TargetProfileId);
            return Result<Comment>.Failure(CommunityError.DatabaseError, "An unexpected error occurred while creating the comment");
        }
    }

    public async Task<IEnumerable<Comment>> Handle(GetCommentsByTargetProfileIdQuery query, CancellationToken cancellationToken = default)
    {
        return await commentRepository.FindByTargetProfileIdAsync(query.TargetProfileId, cancellationToken);
    }

    public async Task<Comment?> Handle(GetCommentByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await commentRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<Result<Comment>> Handle(UpdateCommentCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (command.Rating < 1 || command.Rating > 5)
                return Result<Comment>.Failure(CommunityError.InvalidComment, "Rating must be strictly between 1 and 5");

            var comment = await commentRepository.FindByIdAsync(command.Id, cancellationToken);
            if (comment == null)
                return Result<Comment>.Failure(CommunityError.NotFound, "The comment was not found");
            comment.UpdateContent(command.Content, command.Rating);
            commentRepository.Update(comment);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Comment>.Success(comment);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while updating comment {CommentId}", command.Id);
            return Result<Comment>.Failure(CommunityError.InvalidComment, "The provided comment data is invalid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating comment {CommentId}", command.Id);
            return Result<Comment>.Failure(CommunityError.DatabaseError, "An unexpected error occurred while updating the comment");
        }
    }

    public async Task<bool> Handle(DeleteCommentCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await commentRepository.FindByIdAsync(command.Id, cancellationToken);
            if (comment == null)
                return false;
            commentRepository.Remove(comment);
            await unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting comment {Id}", command.Id);
            return false;
        }
    }

    public async Task<Result<IEnumerable<Comment>>> Handle(GetCommentsByProductIdQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await productRepository.FindByIdAsync(query.ProductId, cancellationToken);
            if (product == null)
                return Result<IEnumerable<Comment>>.Failure(CommunityError.NotFound, "Product not found");

            if (query.Rating.HasValue && (query.Rating.Value < 1 || query.Rating.Value > 5))
                return Result<IEnumerable<Comment>>.Failure(CommunityError.InvalidComment, "Rating filter must be between 1 and 5");

            var comments = await commentRepository.FindByProductIdAsync(query.ProductId, query.Rating, cancellationToken);
            return Result<IEnumerable<Comment>>.Success(comments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving comments for ProductId {ProductId}", query.ProductId);
            return Result<IEnumerable<Comment>>.Failure(CommunityError.DatabaseError, "An unexpected error occurred while retrieving comments");
        }
    }

    public async Task<Result<ProductRatingSummary>> Handle(GetProductRatingSummaryQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await productRepository.FindByIdAsync(query.ProductId, cancellationToken);
            if (product == null)
                return Result<ProductRatingSummary>.Failure(CommunityError.NotFound, "Product not found");

            var summary = await commentRepository.GetRatingSummaryByProductIdAsync(query.ProductId, cancellationToken);
            return Result<ProductRatingSummary>.Success(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving rating summary for ProductId {ProductId}", query.ProductId);
            return Result<ProductRatingSummary>.Failure(CommunityError.DatabaseError, "An unexpected error occurred while retrieving rating summary");
        }
    }
}
