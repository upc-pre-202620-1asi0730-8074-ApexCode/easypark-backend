using EasyPark.Platform.Iam.Application.CommandServices;
using EasyPark.Platform.Iam.Application.Internal.OutboundServices;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.Commands;
using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.Iam.Domain.Repositories;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Iam.Application.Internal.CommandServices;

public class UserAccountCommandService(
    IUserAccountRepository userAccountRepository,
    IHashingService hashingService,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IUserAccountCommandService
{
    public async Task<UserAccount> Handle(SignUpCommand command, CancellationToken cancellationToken = default)
    {
        var email = EmailAddress.Create(command.Email);
        UserAccount.EnsurePasswordPolicy(command.Password, "invalid-password");
        var role = EnumCodes.Parse<UserRole>(command.Role, "invalid-role", "role");
        if (!UserAccount.CanSelfRegisterAs(role))
            throw DomainException.BadRequest("invalid-role",
                "Self-registration is only available for the DRIVER and OPERATOR_ADMIN roles.");

        if (await userAccountRepository.ExistsByEmailAsync(email, cancellationToken))
            throw DomainException.Conflict("email-taken", "The e-mail address is already registered.");

        var userAccount = new UserAccount(email, hashingService.HashPassword(command.Password!), role);
        await userAccountRepository.AddAsync(userAccount, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return userAccount;
    }

    public async Task<(UserAccount userAccount, string token)> Handle(SignInCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!EmailAddress.TryCreate(command.Email, out var email))
            throw DomainException.Unauthorized("invalid-credentials", "Invalid credentials.");

        var userAccount = await userAccountRepository.FindByEmailAsync(email, cancellationToken);
        if (userAccount is null || !hashingService.VerifyPassword(command.Password ?? string.Empty,
                userAccount.PasswordHash))
            throw DomainException.Unauthorized("invalid-credentials", "Invalid credentials.");

        if (!userAccount.IsActive)
            throw DomainException.Forbidden("inactive-account", "The account is not active.");

        userAccount.RegisterSignIn(DateTime.UtcNow);
        await unitOfWork.CompleteAsync(cancellationToken);
        return (userAccount, tokenService.GenerateToken(userAccount));
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        var userAccount = await userAccountRepository.FindByIdAsync(command.UserAccountId, cancellationToken)
                          ?? throw DomainException.Unauthorized("invalid-token",
                              "The account associated with this session no longer exists.");
        if (!userAccount.IsActive)
            throw DomainException.Forbidden("inactive-account", "The account is not active.");
        if (!hashingService.VerifyPassword(command.CurrentPassword ?? string.Empty, userAccount.PasswordHash))
            throw DomainException.Unprocessable("wrong-current-password", "The current password is incorrect.");

        UserAccount.EnsurePasswordPolicy(command.NewPassword, "invalid-new-password");
        userAccount.ChangePasswordHash(hashingService.HashPassword(command.NewPassword!));
        await unitOfWork.CompleteAsync(cancellationToken);
    }
}
