using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;
[SwaggerSchema(Description = "Request payload to update a comment")]
public record UpdateCommentResource([Required][SwaggerParameter(Description = "Content of the comment")] string Content, [Required][Range(1, 5)][SwaggerParameter(Description = "Rating score (1-5)")] int Rating);
