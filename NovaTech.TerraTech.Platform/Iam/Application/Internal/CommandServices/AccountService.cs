using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Iam.Domain.Repository;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Iam.Application.Internal.CommandServices;

public class AccountService(IUserRepository users, IHashingService hashing, ITokenService tokens, IUnitOfWork unitOfWork, AccountEmailService emails)
{
    public async Task<User> Register(string fullName, string emailAddress, string password, string confirmPassword, CancellationToken ct)
    {
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            throw new ApiFailure(400, "PASSWORD_CONFIRMATION_MISMATCH", "Passwords do not match.");
        if (fullName.Trim().Length < 2)
            throw new ApiFailure(400, "INVALID_NAME", "Name requires at least two characters.");
        var email = new Email(emailAddress.Trim().ToLowerInvariant());
        if (await users.ExistsByEmailAsync(email, ct))
            throw new ApiFailure(409, "EMAIL_EXISTS", "Email is already registered.");
        emails.EnsureConfigured();
        var user = new User(email, hashing.HashPassword(password));
        user.RequireEmailVerification();
        user.SetFullName(fullName);
        await users.AddAsync(user, ct);
        try
        {
            await unitOfWork.CompleteAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        {
            throw new ApiFailure(409, "EMAIL_EXISTS", "Email is already registered.");
        }

        await emails.RequestVerification(user.EmailAddress.Value, ct);
        return user;
    }

    public async Task<(User user, string token)> Login(string emailAddress, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(new Email(emailAddress.Trim().ToLowerInvariant()), ct);
        if (user is null || !hashing.VerifyPassword(password, user.PasswordHash))
            throw new ApiFailure(401, "INVALID_CREDENTIALS", "Invalid credentials.");
        if (!user.EmailVerificationExempt && user.EmailVerifiedAtUtc is null)
            throw new ApiFailure(403, "EMAIL_NOT_VERIFIED", "Please verify your email before signing in.");
        return (user, tokens.GenerateToken(user));
    }

    public async Task<User> GetOwn(int callerId, int requestedId, string? email, CancellationToken ct)
    {
        if (callerId != requestedId)
            throw new ApiFailure(404, "USER_NOT_FOUND", "User was not found.");
        var user = await users.FindByIdAsync(callerId, ct);
        if (user is null || email != null && user.EmailAddress.Value != email)
            throw new ApiFailure(404, "USER_NOT_FOUND", "User was not found.");
        return user;
    }
}
