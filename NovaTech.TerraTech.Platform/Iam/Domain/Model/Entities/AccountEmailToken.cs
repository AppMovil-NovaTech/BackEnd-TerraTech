using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
namespace NovaTech.TerraTech.Platform.Iam.Domain.Model.Entities;

public class AccountEmailToken
{
    public int UserId { get; set; }
    public AccountEmailPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}
