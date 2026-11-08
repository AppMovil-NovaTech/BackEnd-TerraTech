using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NovaTech.TerraTech.Platform.Iam.Application.CommandServices;
using NovaTech.TerraTech.Platform.Iam.Application.Internal.OutboundServices;
using NovaTech.TerraTech.Platform.Iam.Domain.Model;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.Iam.Domain.Repository;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Resources.Errors;

namespace NovaTech.TerraTech.Platform.Iam.Application.Internal.CommandServices;

public class UserCommandService(IUserRepository userRepository, ITokenService tokenService, IHashingService hashingService, IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer) : IUserCommandService
{
    private readonly IStringLocalizer<ErrorMessages> _localizer = localizer;
    public async Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(new Email(command.Email), cancellationToken);
        if (user is null || !hashingService.VerifyPassword(command.Password, user.PasswordHash) || !user.CanSignIn())
        {
            return Result<(User user, string token)>.Failure(IamError.InvalidCredentials, _localizer[nameof(IamError.InvalidCredentials)]);
        }

        var token = tokenService.GenerateToken(user);
        return Result<(User user, string token)>.Success((user, token));
    }

    public async Task<Result> Handle(SignUpCommand command, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(new Email(command.Email), cancellationToken))
        {
            return Result.Failure(IamError.UsernameAlreadyTaken, _localizer[nameof(IamError.UsernameAlreadyTaken), command.Email]);
        }

        var hashedPassword = hashingService.HashPassword(command.Password);
        var user = new User(new Email(command.Email), hashedPassword);
        try
        {
            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(IamError.OperationCancelled, _localizer[nameof(IamError.OperationCancelled)]);
        }
        catch (DbUpdateException)
        {
            return Result.Failure(IamError.DatabaseError, _localizer[nameof(IamError.DatabaseError)]);
        }
        catch (Exception)
        {
            return Result.Failure(IamError.InternalServerError, _localizer[nameof(IamError.InternalServerError)]);
        }
    }
}
