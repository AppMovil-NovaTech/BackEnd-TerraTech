using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Entities;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Iam.Domain.Repository;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public sealed class AccountEmailTokenRepository(AppDbContext context) : IAccountEmailTokenRepository
{
    public Task<AccountEmailToken?> FindAsync(int userId, AccountEmailPurpose purpose, CancellationToken cancellationToken) =>
        context.Set<AccountEmailToken>().SingleOrDefaultAsync(token => token.UserId == userId && token.Purpose == purpose, cancellationToken);

    public Task<AccountEmailToken?> FindActiveAsync(string tokenHash, AccountEmailPurpose purpose, DateTime now, CancellationToken cancellationToken) =>
        ActiveTokens(tokenHash, purpose, now).AsNoTracking().SingleOrDefaultAsync(cancellationToken);

    public async Task AddAsync(AccountEmailToken token, CancellationToken cancellationToken) =>
        await context.Set<AccountEmailToken>().AddAsync(token, cancellationToken);

    public async Task<bool> TryDeleteActiveAsync(string tokenHash, AccountEmailPurpose purpose, DateTime now, CancellationToken cancellationToken) =>
        await ActiveTokens(tokenHash, purpose, now).ExecuteDeleteAsync(cancellationToken) == 1;

    private IQueryable<AccountEmailToken> ActiveTokens(string tokenHash, AccountEmailPurpose purpose, DateTime now) =>
        context.Set<AccountEmailToken>().Where(token => token.TokenHash == tokenHash && token.Purpose == purpose && token.ExpiresAtUtc > now);
}
