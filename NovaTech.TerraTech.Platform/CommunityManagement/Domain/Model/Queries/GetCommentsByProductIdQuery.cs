namespace NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Queries;

public record GetCommentsByProductIdQuery(int ProductId, int? Rating = null);
