using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Iam.Domain.Repository;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace NovaTech.TerraTech.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    public async Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return await Context.Set<User>().FirstOrDefaultAsync(user => user.EmailAddress.Value == email.Value, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return await Context.Set<User>().AnyAsync(user => user.EmailAddress.Value == email.Value, cancellationToken);
    }

    public async Task LockAsync(int userId, CancellationToken cancellationToken) =>
        await Context.Set<User>().Where(user => user.Id == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.TokenVersion, user => user.TokenVersion), cancellationToken);

    public async Task ConfirmEmailAsync(int userId, DateTime verifiedAt, CancellationToken cancellationToken) =>
        await Context.Set<User>().Where(user => user.Id == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.EmailVerifiedAtUtc, verifiedAt), cancellationToken);

    public async Task ResetPasswordAsync(int userId, string passwordHash, CancellationToken cancellationToken) =>
        await Context.Set<User>().Where(user => user.Id == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.PasswordHash, passwordHash)
                .SetProperty(user => user.TokenVersion, user => user.TokenVersion + 1), cancellationToken);

    public async Task<IEnumerable<User>> FindByIdsAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
            return [];
        return await Context.Set<User>()
            .Where(user => idList.Contains(user.Id))
            .ToListAsync(cancellationToken);
    }
}
