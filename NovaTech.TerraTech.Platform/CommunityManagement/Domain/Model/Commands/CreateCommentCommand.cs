namespace NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Commands;
public record CreateCommentCommand(int UserId, int ProductId, int TargetProfileId, string Content, int Rating);
