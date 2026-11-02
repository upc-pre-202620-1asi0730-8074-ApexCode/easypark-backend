using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Shared.Domain.Model;

/// <summary>
///     Small helpers used by aggregates to validate their invariants.
/// </summary>
public static class Guard
{
    public static string RequiredText(string? value, int maxLength, string errorCode, string fieldName)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw DomainException.BadRequest(errorCode, $"The field '{fieldName}' is required.");
        if (trimmed.Length > maxLength)
            throw DomainException.BadRequest(errorCode, $"The field '{fieldName}' cannot exceed {maxLength} characters.");
        return trimmed;
    }

    public static string? OptionalText(string? value, int maxLength, string errorCode, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > maxLength)
            throw DomainException.BadRequest(errorCode, $"The field '{fieldName}' cannot exceed {maxLength} characters.");
        return trimmed;
    }

    public static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
