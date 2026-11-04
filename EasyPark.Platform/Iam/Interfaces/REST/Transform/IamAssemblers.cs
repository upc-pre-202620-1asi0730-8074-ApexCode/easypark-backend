using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.Commands;
using EasyPark.Platform.Iam.Interfaces.REST.Resources;
using EasyPark.Platform.Shared.Domain.Model;

namespace EasyPark.Platform.Iam.Interfaces.REST.Transform;

public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommandFromResource(SignUpResource resource)
    {
        return new SignUpCommand(resource.Email, resource.Password, resource.Role);
    }
}

public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommandFromResource(SignInResource resource)
    {
        return new SignInCommand(resource.Email, resource.Password);
    }
}

public static class ChangePasswordCommandFromResourceAssembler
{
    public static ChangePasswordCommand ToCommandFromResource(int userAccountId, ChangePasswordResource resource)
    {
        return new ChangePasswordCommand(userAccountId, resource.CurrentPassword, resource.NewPassword);
    }
}

public static class UserAccountResourceFromEntityAssembler
{
    public static UserAccountResource ToResourceFromEntity(UserAccount userAccount)
    {
        return new UserAccountResource(userAccount.Id, userAccount.Email, EnumCodes.ToCode(userAccount.Role));
    }

    public static AuthenticatedUserAccountResource ToAuthenticatedResource(UserAccount userAccount, string token)
    {
        return new AuthenticatedUserAccountResource(userAccount.Id, userAccount.Email,
            EnumCodes.ToCode(userAccount.Role), token);
    }
}
