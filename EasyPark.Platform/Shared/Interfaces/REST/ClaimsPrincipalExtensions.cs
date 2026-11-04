using System.Security.Claims;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Shared.Interfaces.REST;

/// <summary>
///     Reads the authenticated user from the JWT claims (<c>sub</c> and <c>role</c>).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public const string SubjectClaim = "sub";
    public const string RoleClaim = "role";
    public const string EmailClaim = "email";

    public const string OperatorRoles = "OPERATOR_ADMIN,OPERATOR_STAFF";
    public const string DriverRole = "DRIVER";

    public static int GetUserAccountId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(SubjectClaim)?.Value;
        if (!int.TryParse(value, out var userAccountId) || userAccountId <= 0)
            throw DomainException.Unauthorized("invalid-token", "The access token does not identify a user account.");
        return userAccountId;
    }

    public static string GetRoleCode(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(RoleClaim)?.Value ?? string.Empty;
    }

    public static bool IsOperator(this ClaimsPrincipal principal)
    {
        var role = principal.GetRoleCode();
        return role is "OPERATOR_ADMIN" or "OPERATOR_STAFF";
    }
}
