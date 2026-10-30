using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Entities;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Tb1;
namespace NovaTech.TerraTech.Platform.Iam.Application.Internal.CommandServices;

public class AccountEmailService(AppDbContext db, IAccountEmailSender sender, IHashingService hashing, TimeProvider clock)
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
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var userId = await Consume(token, AccountEmailPurpose.Verification, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        await db.Set<User>().Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailVerifiedAtUtc, now), ct);
        await transaction.CommitAsync(ct);
    }
    public async Task ResetPassword(string token, string password, string confirmation, CancellationToken ct)
    {
        if (password.Length is < 6 or > 128)
            throw new ApiFailure(400, "INVALID_PASSWORD", "Password must contain 6 to 128 characters.");
        if (password != confirmation)
            throw new ApiFailure(400, "PASSWORD_CONFIRMATION_MISMATCH", "Passwords do not match.");
        var hash = hashing.HashPassword(password);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var userId = await Consume(token, AccountEmailPurpose.PasswordReset, ct);
        await db.Set<User>().Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, hash)
                .SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1), ct);
        await transaction.CommitAsync(ct);
    }
    private Task<User?> FindUser(string email, CancellationToken ct)
    {
        var normalized = new Email(email).Value;
        return db.Set<User>().SingleOrDefaultAsync(u => u.EmailAddress.Value == normalized, ct);
    }
    private async Task Issue(User user, AccountEmailPurpose purpose, TimeSpan lifetime, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize issuance for one account, including the first request without a token row.
        await db.Set<User>().Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion), ct);
        var previous = await db.Set<AccountEmailToken>().SingleOrDefaultAsync(t => t.UserId == user.Id && t.Purpose == purpose, ct);
        if (previous is not null && now - previous.RequestedAtUtc < TimeSpan.FromMinutes(1)) return;
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var record = previous ?? new AccountEmailToken { UserId = user.Id, Purpose = purpose };
        record.TokenHash = Hash(token);
        record.ExpiresAtUtc = now.Add(lifetime);
        record.RequestedAtUtc = now;
        if (previous is null) db.Add(record);
        await db.SaveChangesAsync(ct);
        if (purpose == AccountEmailPurpose.Verification) await sender.SendVerification(user.EmailAddress.Value, token, ct);
        else await sender.SendPasswordReset(user.EmailAddress.Value, token, ct);
        await transaction.CommitAsync(ct);
    }
    private async Task<int> Consume(string token, AccountEmailPurpose purpose, CancellationToken ct)
    {
        if (token.Length != 43) throw InvalidToken();
        var hash = Hash(token);
        var now = clock.GetUtcNow().UtcDateTime;
        var query = db.Set<AccountEmailToken>().Where(t => t.TokenHash == hash && t.Purpose == purpose && t.ExpiresAtUtc > now);
        var record = await query.AsNoTracking().SingleOrDefaultAsync(ct);
        if (record is null || await query.ExecuteDeleteAsync(ct) != 1) throw InvalidToken();
        return record.UserId;
    }
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static ApiFailure InvalidToken() => new(400, "TOKEN_INVALID_OR_EXPIRED", "This link is invalid or expired. Please request another.");
}
