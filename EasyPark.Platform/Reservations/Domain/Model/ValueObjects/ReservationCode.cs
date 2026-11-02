using System.Security.Cryptography;

namespace EasyPark.Platform.Reservations.Domain.Model.ValueObjects;

/// <summary>
///     Human-readable reservation code, for example <c>EP-7K2Q9XHM</c>. It is shown to the driver and typed or
///     scanned at the gate.
/// </summary>
public static class ReservationCode
{
    public const int MaxLength = 20;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        return "EP-" + RandomNumberGenerator.GetString(Alphabet, 8);
    }

    public static string? Normalize(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
