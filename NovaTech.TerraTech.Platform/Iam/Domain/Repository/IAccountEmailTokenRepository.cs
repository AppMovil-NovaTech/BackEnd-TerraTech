using NovaTech.TerraTech.Platform.Iam.Domain.Model.Entities;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;

namespace NovaTech.TerraTech.Platform.Iam.Domain.Repository;

public interface IAccountEmailTokenRepository
{
    Task<AccountEmailToken?> FindAsync(int userId, AccountEmailPurpose purpose, CancellationToken cancellationToken);
    Task<AccountEmailToken?> FindActiveAsync(string tokenHash, AccountEmailPurpose purpose, DateTime now, CancellationToken cancellationToken);
    Task AddAsync(AccountEmailToken token, CancellationToken cancellationToken);
    Task<bool> TryDeleteActiveAsync(string tokenHash, AccountEmailPurpose purpose, DateTime now, CancellationToken cancellationToken);
}
