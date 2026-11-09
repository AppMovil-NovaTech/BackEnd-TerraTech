using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.CommunityManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommunityManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Transform;
using NovaTech.TerraTech.Platform.Iam.Interface.Acl;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST;

[ApiController]
[Route("api/v1/comments")]
[Produces("application/json")]
[Tags("Communities")]
public class CommentsController(
    ICommentService commentService,
    IIamContextFacade iamContextFacade,
    ILogger<CommentsController> logger) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Creates a new comment", Description = "Creates a comment for a profile")]
    [SwaggerResponse(201, "Comment created", typeof(CommentResource))]
    [SwaggerResponse(400, "Invalid request", typeof(string))]
    [SwaggerResponse(500, "Unexpected error", typeof(ProblemDetails))]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = CreateCommentCommandFromResourceAssembler.ToCommandFromResource(resource);
            var result = await commentService.Handle(command, cancellationToken);
            if (result.IsSuccess)
            {
                var authorName = await iamContextFacade.FetchFullNameByUserId(result.Value.UserId, cancellationToken);
                return CreatedAtAction(nameof(GetCommentById), new { id = result.Value.Id }, CommentResourceFromEntityAssembler.ToResourceFromEntity(result.Value, authorName));
            }

            return (CommunityError)result.Error switch
            {
                CommunityError.SelfReviewNotAllowed => BadRequest("Producers cannot review their own products"),
                CommunityError.InvalidComment or CommunityError.InvalidProfileId => BadRequest("Invalid comment request data"),
                _ => Problem(title: "Unexpected server error", detail: "An unexpected error occurred", statusCode: 500)
            };
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating comment");
            return Problem(title: "Unexpected server error", detail: "An unexpected error occurred", statusCode: 500);
        }
    }

    [AllowAnonymous]
    [HttpGet("~/api/v1/community-profiles/{targetProfileId}/comments")]
    [SwaggerOperation(Summary = "Gets comments by target profile")]
    [SwaggerResponse(200, "Comments retrieved", typeof(IEnumerable<CommentResource>))]
    public async Task<IActionResult> GetCommentsByTargetProfileId(int targetProfileId, CancellationToken cancellationToken)
    {
        var query = new GetCommentsByTargetProfileIdQuery(targetProfileId);
        var comments = (await commentService.Handle(query, cancellationToken)).ToList();
        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        var authorNames = await iamContextFacade.FetchFullNamesByUserIds(userIds, cancellationToken);
        var resources = comments.Select(c => CommentResourceFromEntityAssembler.ToResourceFromEntity(
            c,
            authorNames.TryGetValue(c.UserId, out var name) ? name : "Usuario"));
        return Ok(resources);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Gets a comment by id")]
    [SwaggerResponse(200, "Comment found", typeof(CommentResource))]
    [SwaggerResponse(404, "Comment not found")]
    public async Task<IActionResult> GetCommentById(int id, CancellationToken cancellationToken)
    {
        var query = new GetCommentByIdQuery(id);
        var comment = await commentService.Handle(query, cancellationToken);
        if (comment == null)
            return NotFound();
        var authorName = await iamContextFacade.FetchFullNameByUserId(comment.UserId, cancellationToken);
        var resource = CommentResourceFromEntityAssembler.ToResourceFromEntity(comment, authorName);
        return Ok(resource);
    }

    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Updates a comment", Description = "Updates the content and rating of an existing comment")]
    [SwaggerResponse(200, "Comment updated", typeof(CommentResource))]
    [SwaggerResponse(404, "Comment not found", typeof(string))]
    [SwaggerResponse(500, "Unexpected error", typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentResource resource, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateCommentCommand(id, resource.Content, resource.Rating);
            var result = await commentService.Handle(command, cancellationToken);
            if (result.IsSuccess)
            {
                var authorName = await iamContextFacade.FetchFullNameByUserId(result.Value.UserId, cancellationToken);
                return Ok(CommentResourceFromEntityAssembler.ToResourceFromEntity(result.Value, authorName));
            }

            return (CommunityError)result.Error switch
            {
                CommunityError.NotFound => NotFound("Comment not found"),
                CommunityError.InvalidComment => BadRequest("Invalid comment request data"),
                _ => Problem(title: "Unexpected server error", statusCode: 500)
            };
        }
        catch (ApiFailure)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating comment {Id}", id);
            return Problem(title: "Unexpected server error", statusCode: 500);
        }
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Deletes a comment", Description = "Deletes a comment by its ID")]
    [SwaggerResponse(204, "Comment deleted")]
    [SwaggerResponse(404, "Comment not found")]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken cancellationToken)
    {
        var command = new DeleteCommentCommand(id);
        var result = await commentService.Handle(command, cancellationToken);
        if (!result)
            return NotFound("Comment not found");
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("~/api/v1/products/{productId}/comments")]
    [SwaggerOperation(Summary = "Gets comments for a product", Description = "Returns comments ordered by creation date descending with optional rating filter")]
    [SwaggerResponse(200, "Comments retrieved", typeof(IEnumerable<CommentResource>))]
    [SwaggerResponse(400, "Invalid rating filter")]
    [SwaggerResponse(404, "Product not found")]
    public async Task<IActionResult> GetCommentsByProductId(int productId, [FromQuery] int? rating, CancellationToken cancellationToken)
    {
        var query = new GetCommentsByProductIdQuery(productId, rating);
        var result = await commentService.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return (CommunityError)result.Error! switch
            {
                CommunityError.NotFound => NotFound(result.Message),
                CommunityError.InvalidComment => BadRequest(result.Message),
                _ => Problem(title: "Unexpected server error", statusCode: 500)
            };
        }

        var comments = result.Value!.ToList();
        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        var authorNames = await iamContextFacade.FetchFullNamesByUserIds(userIds, cancellationToken);

        var resources = comments.Select(c => CommentResourceFromEntityAssembler.ToResourceFromEntity(
            c,
            authorNames.TryGetValue(c.UserId, out var name) ? name : "Usuario"));

        return Ok(resources);
    }

    [AllowAnonymous]
    [HttpGet("~/api/v1/products/{productId}/ratings-summary")]
    [SwaggerOperation(Summary = "Gets rating summary for a product", Description = "Returns average rating, review count, and star rating breakdown")]
    [SwaggerResponse(200, "Rating summary retrieved", typeof(ProductRatingSummaryResource))]
    [SwaggerResponse(404, "Product not found")]
    public async Task<IActionResult> GetProductRatingSummary(int productId, CancellationToken cancellationToken)
    {
        var query = new GetProductRatingSummaryQuery(productId);
        var result = await commentService.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return (CommunityError)result.Error! switch
            {
                CommunityError.NotFound => NotFound(result.Message),
                _ => Problem(title: "Unexpected server error", statusCode: 500)
            };
        }

        var resource = ProductRatingSummaryResourceFromEntityAssembler.ToResourceFromEntity(result.Value!);
        return Ok(resource);
    }
}
