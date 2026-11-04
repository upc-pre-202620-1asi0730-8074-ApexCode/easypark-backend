using System.Text.RegularExpressions;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     E-mail address normalized to lower case. Same validation rule used by the Web Application.
/// </summary>
public record EmailAddress
{
    public const int MaxLength = 254;

    private static readonly Regex EmailPattern =
        new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string? value)
    {
        if (TryCreate(value, out var email)) return email;
        throw DomainException.BadRequest("invalid-email", "A valid e-mail address is required.");
    }

    public static bool TryCreate(string? value, out EmailAddress email)
    {
        email = null!;
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length is 0 or > MaxLength || !EmailPattern.IsMatch(normalized)) return false;
        email = new EmailAddress(normalized);
        return true;
    }

    /// <summary>
    ///     Used only by the persistence layer to rebuild a value that was already validated.
    /// </summary>
    public static EmailAddress FromDatabase(string value)
    {
        return new EmailAddress(value);
    }

    public override string ToString()
    {
        return Value;
    }
}
