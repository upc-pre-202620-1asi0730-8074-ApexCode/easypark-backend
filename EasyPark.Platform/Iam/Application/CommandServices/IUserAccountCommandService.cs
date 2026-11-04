using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.Commands;

namespace EasyPark.Platform.Iam.Application.CommandServices;

public interface IUserAccountCommandService
{
    Task<UserAccount> Handle(SignUpCommand command, CancellationToken cancellationToken = default);

    Task<(UserAccount userAccount, string token)> Handle(SignInCommand command,
        CancellationToken cancellationToken = default);

    Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken = default);
}
