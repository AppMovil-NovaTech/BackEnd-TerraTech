namespace NovaTech.TerraTech.Platform.Shared.Domain.Repositories;

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
