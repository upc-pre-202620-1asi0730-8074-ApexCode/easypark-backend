using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     User account aggregate root of the IAM bounded context.
/// </summary>
public class UserAccount
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    public static readonly IReadOnlyCollection<UserRole> SelfRegistrationRoles =
        new[] { UserRole.Driver, UserRole.OperatorAdmin };

    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected UserAccount()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public UserAccount(EmailAddress email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("The password hash is required.", nameof(passwordHash));
        Email = email.Value;
        PasswordHash = passwordHash;
        Role = role;
        Status = AccountStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    /// <summary>Normalized e-mail (validated through the <see cref="EmailAddress" /> value object).</summary>
    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public AccountStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    public bool IsActive => Status == AccountStatus.Active;

    public bool IsOperator => Role is UserRole.OperatorAdmin or UserRole.OperatorStaff;

    public static bool CanSelfRegisterAs(UserRole role)
    {
        return SelfRegistrationRoles.Contains(role);
    }

    /// <summary>
    ///     Validates a plain-text password before hashing it.
    /// </summary>
    public static void EnsurePasswordPolicy(string? password, string errorCode)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            throw DomainException.BadRequest(errorCode,
                $"The password must contain at least {MinPasswordLength} characters.");
        if (password.Length > MaxPasswordLength)
            throw DomainException.BadRequest(errorCode,
                $"The password cannot exceed {MaxPasswordLength} characters.");
    }

    public void RegisterSignIn(DateTime signedInAt)
    {
        LastLoginAt = signedInAt;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("The password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public void Deactivate()
    {
        Status = AccountStatus.Inactive;
    }
}
