using System.Text;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Shared.Domain.Model;

/// <summary>
///     Converts C# enum members (PascalCase) to the codes used by the REST contract and the
///     database (UPPER_SNAKE_CASE), for example <c>OperatorAdmin</c> to <c>OPERATOR_ADMIN</c>.
/// </summary>
public static class EnumCodes
{
    public static string ToCode<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return ToUpperSnakeCase(value.ToString());
    }

    public static bool TryParse<TEnum>(string? code, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(code)) return false;
        var normalized = code.Trim();
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (!string.Equals(ToCode(candidate), normalized, StringComparison.OrdinalIgnoreCase)) continue;
            value = candidate;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Parses a code received from a client. Throws a <see cref="DomainException" /> (400) when it is not valid.
    /// </summary>
    public static TEnum Parse<TEnum>(string? code, string errorCode, string fieldName) where TEnum : struct, Enum
    {
        if (TryParse<TEnum>(code, out var value)) return value;
        var allowed = string.Join(", ", Enum.GetValues<TEnum>().Select(item => ToCode(item)));
        throw DomainException.BadRequest(errorCode, $"The field '{fieldName}' must be one of: {allowed}.");
    }

    /// <summary>
    ///     Parses a code stored in the database. Used by EF Core value converters.
    /// </summary>
    public static TEnum FromDatabase<TEnum>(string code) where TEnum : struct, Enum
    {
        if (TryParse<TEnum>(code, out var value)) return value;
        throw new InvalidOperationException($"The value '{code}' is not a valid {typeof(TEnum).Name}.");
    }

    private static string ToUpperSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (index > 0 && char.IsUpper(character) &&
                (char.IsLower(name[index - 1]) || char.IsDigit(name[index - 1])))
                builder.Append('_');
            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}
