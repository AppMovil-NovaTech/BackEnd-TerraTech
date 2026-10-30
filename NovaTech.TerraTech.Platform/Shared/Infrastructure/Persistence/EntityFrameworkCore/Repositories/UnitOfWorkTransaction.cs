using Microsoft.EntityFrameworkCore.Storage;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public sealed class UnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
