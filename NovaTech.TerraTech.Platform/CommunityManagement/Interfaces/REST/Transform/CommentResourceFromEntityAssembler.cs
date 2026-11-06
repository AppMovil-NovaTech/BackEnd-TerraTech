using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Transform;
public static class CommentResourceFromEntityAssembler
{
    public static CommentResource ToResourceFromEntity(Comment entity)
    {
        return new CommentResource(entity.Id, entity.UserId, entity.ProductId, entity.TargetProfileId, entity.Content, entity.Rating, entity.CreatedAt);
    }
}
