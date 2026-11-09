namespace NovaTech.TerraTech.Platform.Iam.Interface.Acl;

public interface IIamContextFacade
{
    Task<int> CreateUser(string email, string password, CancellationToken cancellationToken);
    Task<int> FetchUserByEmail(string email, CancellationToken cancellationToken);
    Task<string> FetchEmailByUserId(int userId, CancellationToken cancellationToken);
    Task<string?> FetchFullNameByUserId(int userId, CancellationToken cancellationToken = default);
    Task<IDictionary<int, string>> FetchFullNamesByUserIds(IEnumerable<int> userIds, CancellationToken cancellationToken = default);
}
