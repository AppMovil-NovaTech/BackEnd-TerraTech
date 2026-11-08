using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Commands;

namespace NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
public partial class Comment
{
    protected Comment()
    {
        Content = string.Empty;
    }

    public Comment(CreateCommentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UserId <= 0)
            throw new ArgumentException("UserId must be greater than zero", nameof(command));
        if (command.ProductId <= 0)
            throw new ArgumentException("ProductId must be greater than zero", nameof(command));
        if (command.TargetProfileId <= 0)
            throw new ArgumentException("TargetProfileId must be greater than zero", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Content))
            throw new ArgumentException("Content is required", nameof(command));
        if (command.Rating < 1 || command.Rating > 5)
            throw new ArgumentException("Rating must be strictly between 1 and 5", nameof(command));

        UserId = command.UserId;
        ProductId = command.ProductId;
        TargetProfileId = command.TargetProfileId;
        Content = command.Content.Trim();
        Rating = command.Rating;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int ProductId { get; private set; }
    public int TargetProfileId { get; private set; }
    public string Content { get; private set; }
    public int Rating { get; private set; }

    public void UpdateContent(string content, int rating)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be strictly between 1 and 5", nameof(rating));
        Content = content.Trim();
        Rating = rating;
    }
}
