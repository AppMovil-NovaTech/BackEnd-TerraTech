using System.Text.Json.Serialization;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;

public partial class User(Email emailAddress, string passwordHash)
{
    public User() : this(null!, string.Empty)
    {
    }

    public bool EmailVerificationExempt { get; set; }
    public DateTime? EmailVerifiedAtUtc { get; set; }
    public int TokenVersion { get; set; }
    public bool CanSignIn() => EmailVerificationExempt || EmailVerifiedAtUtc is not null;
    public string? FullName { get; private set; }

    public void SetFullName(string name) => FullName = name.Trim();
    public int Id { get; }
    public Email EmailAddress { get; private set; } = emailAddress;

    [JsonIgnore]
    public string PasswordHash { get; private set; } = passwordHash;

    public User UpdateEmail(Email newEmail)
    {
        EmailAddress = newEmail;
        return this;
    }

    public User UpdatePasswordHash(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        return this;
    }
}
