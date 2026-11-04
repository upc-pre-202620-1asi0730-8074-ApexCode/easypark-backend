using System.Text.RegularExpressions;

namespace EasyPark.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    ///     "ParkingFacilities" becomes "parking-facilities".
    /// </summary>
    public static string ToKebabCase(this string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return KebabRegex().Replace(text, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex KebabRegex();
}
