using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaTech.TerraTech.Platform.CommunityManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommunityManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST;
[ApiController]
[Route("api/v1/comments")]
[Produces("application/json")]
[Tags("Communities")]
public class CommentsController(ICommentService commentService, ILogger<CommentsController> logger) : ControllerBase
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
                return CreatedAtAction(nameof(GetCommentById), new { id = result.Value.Id }, CommentResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
            }

            return (CommunityError)result.Error switch
            {
                CommunityError.SelfReviewNotAllowed => BadRequest("Producers cannot review their own products"),
                CommunityError.InvalidComment or CommunityError.InvalidProfileId => BadRequest("Invalid comment request data"),
                _ => Problem(title: "Unexpected server error", detail: "An unexpected error occurred", statusCode: 500)};
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
        var comments = await commentService.Handle(query, cancellationToken);
        var resources = comments.Select(CommentResourceFromEntityAssembler.ToResourceFromEntity);
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
        var resource = CommentResourceFromEntityAssembler.ToResourceFromEntity(comment);
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
                return Ok(CommentResourceFromEntityAssembler.ToResourceFromEntity(result.Value));
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
}
