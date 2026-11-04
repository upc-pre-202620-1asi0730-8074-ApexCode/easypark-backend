using System.Text.RegularExpressions;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Profiles.Domain.Model.ValueObjects;

/// <summary>
///     Peruvian plate number. Accepts "ABC123", "abc-123" (cars) and "1234AB" (motorcycles) and normalizes
///     them to "ABC-123" / "1234-AB". Same rule implemented in the Web Application (plate-number.js).
/// </summary>
public record PlateNumber
{
    public const int MaxLength = 12;

    private static readonly Regex CarPattern =
        new("^([A-Z0-9]{3})-?([0-9]{3})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MotorcyclePattern =
        new("^([0-9]{4})-?([A-Z]{2})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private PlateNumber(string value, bool isMotorcycle)
    {
        Value = value;
        IsMotorcyclePlate = isMotorcycle;
    }

    public string Value { get; }

    public bool IsMotorcyclePlate { get; }

    public static PlateNumber Create(string? value)
    {
        if (TryCreate(value, out var plateNumber)) return plateNumber;
        throw DomainException.BadRequest("invalid-plate-number",
            "The plate number must look like ABC-123 (car) or 1234-AB (motorcycle).");
    }

    public static bool TryCreate(string? value, out PlateNumber plateNumber)
    {
        plateNumber = null!;
        var compact = Regex.Replace(value ?? string.Empty, @"\s+", string.Empty).ToUpperInvariant();
        var car = CarPattern.Match(compact);
        if (car.Success)
        {
            plateNumber = new PlateNumber($"{car.Groups[1].Value}-{car.Groups[2].Value}", false);
            return true;
        }

        var motorcycle = MotorcyclePattern.Match(compact);
        if (!motorcycle.Success) return false;
        plateNumber = new PlateNumber($"{motorcycle.Groups[1].Value}-{motorcycle.Groups[2].Value}", true);
        return true;
    }

    public override string ToString()
    {
        return Value;
    }
}
