using System.Security.Cryptography;
using System.Text;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Entities;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Iam.Domain.Repository;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Tb1;

namespace NovaTech.TerraTech.Platform.Iam.Application.Internal.CommandServices;

public class AccountEmailService(
    IUserRepository users,
    IAccountEmailTokenRepository tokens,
    IUnitOfWork unitOfWork,
    IAccountEmailSender sender,
    IHashingService hashing,
    TimeProvider clock)
{
    public void EnsureConfigured() => sender.EnsureConfigured();

    public async Task RequestVerification(string email, CancellationToken ct)
    {
        sender.EnsureConfigured();
        var user = await FindUser(email, ct);
        if (user is null || user.EmailVerifiedAtUtc is not null) return;
        await Issue(user, AccountEmailPurpose.Verification, TimeSpan.FromHours(24), ct);
    }

    public async Task RequestPasswordReset(string email, CancellationToken ct)
    {
        sender.EnsureConfigured();
        var user = await FindUser(email, ct);
        if (user is not null) await Issue(user, AccountEmailPurpose.PasswordReset, TimeSpan.FromHours(1), ct);
    }

    public async Task ConfirmVerification(string token, CancellationToken ct)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        var userId = await Consume(token, AccountEmailPurpose.Verification, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        await users.ConfirmEmailAsync(userId, now, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ResetPassword(string token, string password, string confirmation, CancellationToken ct)
    {
        if (password.Length is < 6 or > 128)
            throw new ApiFailure(400, "INVALID_PASSWORD", "Password must contain 6 to 128 characters.");
        if (password != confirmation)
            throw new ApiFailure(400, "PASSWORD_CONFIRMATION_MISMATCH", "Passwords do not match.");
        var hash = hashing.HashPassword(password);
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        var userId = await Consume(token, AccountEmailPurpose.PasswordReset, ct);
        await users.ResetPasswordAsync(userId, hash, ct);
        await transaction.CommitAsync(ct);
    }

    private Task<User?> FindUser(string email, CancellationToken ct) =>
        users.FindByEmailAsync(new Email(email), ct);

    private async Task Issue(User user, AccountEmailPurpose purpose, TimeSpan lifetime, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        await users.LockAsync(user.Id, ct);
        var previous = await tokens.FindAsync(user.Id, purpose, ct);
        if (previous is not null && now - previous.RequestedAtUtc < TimeSpan.FromMinutes(1)) return;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var record = previous ?? new AccountEmailToken { UserId = user.Id, Purpose = purpose };
        record.TokenHash = Hash(token);
        record.ExpiresAtUtc = now.Add(lifetime);
        record.RequestedAtUtc = now;
        if (previous is null) await tokens.AddAsync(record, ct);
        await unitOfWork.CompleteAsync(ct);
        if (purpose == AccountEmailPurpose.Verification) await sender.SendVerification(user.EmailAddress.Value, token, ct);
        else await sender.SendPasswordReset(user.EmailAddress.Value, token, ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<int> Consume(string token, AccountEmailPurpose purpose, CancellationToken ct)
    {
        if (token.Length != 43) throw InvalidToken();
        var hash = Hash(token);
        var now = clock.GetUtcNow().UtcDateTime;
        var record = await tokens.FindActiveAsync(hash, purpose, now, ct);
        if (record is null) throw InvalidToken();
        await users.LockAsync(record.UserId, ct);
        if (!await tokens.TryDeleteActiveAsync(hash, purpose, clock.GetUtcNow().UtcDateTime, ct)) throw InvalidToken();
        return record.UserId;
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static ApiFailure InvalidToken() => new(400, "TOKEN_INVALID_OR_EXPIRED", "This link is invalid or expired. Please request another.");
}
