using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Request payload to create a comment")]
public record CreateCommentResource(
    [Required][SwaggerParameter(Description = "Author user identifier")] int UserId,
    [Required][SwaggerParameter(Description = "Product identifier")] int ProductId,
    [Required][SwaggerParameter(Description = "Target profile identifier")] int TargetProfileId,
    [Required][SwaggerParameter(Description = "Content of the comment")] string Content,
    [Required][Range(1, 5)][SwaggerParameter(Description = "Rating score (1-5)")] int Rating);
