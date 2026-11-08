using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Iam.Domain.Repository;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByEmailAsync(Email emailAddress, CancellationToken cancellationToken);
    Task LockAsync(int userId, CancellationToken cancellationToken);
    Task ConfirmEmailAsync(int userId, DateTime verifiedAt, CancellationToken cancellationToken);
    Task ResetPasswordAsync(int userId, string passwordHash, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(Email emailAddress, CancellationToken cancellationToken);
}
