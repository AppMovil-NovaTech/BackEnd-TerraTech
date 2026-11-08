using NovaTech.TerraTech.Platform.Iam.Application.CommandServices;
using NovaTech.TerraTech.Platform.Iam.Application.QueryServices;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.Iam.Domain.Repository;
using NovaTech.TerraTech.Platform.Iam.Interface.Acl;

namespace NovaTech.TerraTech.Platform.Iam.Application.Acl;

public class IamContextFacade(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService,
    IUserRepository userRepository) : IIamContextFacade
{
    public async Task<int> CreateUser(string email, string password, CancellationToken cancellationToken)
    {
        var signUpCommand = new SignUpCommand(email, password);
        var signUpResult = await userCommandService.Handle(signUpCommand, cancellationToken);
        if (signUpResult.IsFailure)
            return 0;
        var getUserByEmailQuery = new GetUserByEmailQuery(email);
        var result = await userQueryService.Handle(getUserByEmailQuery, cancellationToken);
        return result?.Id ?? 0;
    }

    public async Task<int> FetchUserByEmail(string email, CancellationToken cancellationToken)
    {
        var getUserByEmailQuery = new GetUserByEmailQuery(email);
        var result = await userQueryService.Handle(getUserByEmailQuery, cancellationToken);
        return result?.Id ?? 0;
    }

    public async Task<string> FetchEmailByUserId(int userId, CancellationToken cancellationToken)
    {
        var getUserByIdQuery = new GetUserByIdQuery(userId);
        var result = await userQueryService.Handle(getUserByIdQuery, cancellationToken);
        return result?.EmailAddress.Value ?? string.Empty;
    }

    public async Task<string?> FetchFullNameByUserId(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;
        if (!string.IsNullOrWhiteSpace(user.FullName))
            return user.FullName;
        if (!string.IsNullOrWhiteSpace(user.EmailAddress?.Value))
            return user.EmailAddress.Value;
        return "Usuario";
    }

    public async Task<IDictionary<int, string>> FetchFullNamesByUserIds(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<int, string>();

        var users = await userRepository.FindByIdsAsync(idList, cancellationToken);
        var result = new Dictionary<int, string>();
        foreach (var user in users)
        {
            var name = !string.IsNullOrWhiteSpace(user.FullName)
                ? user.FullName
                : (!string.IsNullOrWhiteSpace(user.EmailAddress?.Value) ? user.EmailAddress.Value : "Usuario");
            result[user.Id] = name;
        }
        return result;
    }
}
