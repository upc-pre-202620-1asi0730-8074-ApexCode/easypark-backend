using System.Globalization;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;

/// <summary>
///     Parking facility managed by an operator profile: location, hourly rate, opening hours and status.
/// </summary>
public class ParkingFacility
{
    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected ParkingFacility()
    {
        Name = string.Empty;
        Address = string.Empty;
        OpenTime = "00:00";
        CloseTime = "23:59";
    }

    public ParkingFacility(int operatorProfileId, string? name, string? address, decimal? latitude,
        decimal? longitude, decimal hourlyRate, string? openTime, string? closeTime, FacilityStatus status)
    {
        if (operatorProfileId <= 0) throw new ArgumentOutOfRangeException(nameof(operatorProfileId));
        OperatorProfileId = operatorProfileId;
        Name = string.Empty;
        Address = string.Empty;
        OpenTime = "00:00";
        CloseTime = "23:59";
        UpdateDetails(name, address, latitude, longitude, hourlyRate, openTime, closeTime);
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public int OperatorProfileId { get; private set; }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public decimal HourlyRate { get; private set; }

    /// <summary>Opening time in HH:mm format.</summary>
    public string OpenTime { get; private set; }

    /// <summary>Closing time in HH:mm format.</summary>
    public string CloseTime { get; private set; }

    public FacilityStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsActive => Status == FacilityStatus.Active;

    public bool IsManagedBy(int operatorProfileId)
    {
        return OperatorProfileId == operatorProfileId;
    }

    public void UpdateDetails(string? name, string? address, decimal? latitude, decimal? longitude,
        decimal hourlyRate, string? openTime, string? closeTime)
    {
        if (hourlyRate < 0 || hourlyRate > 1000)
            throw DomainException.BadRequest("invalid-facility", "The hourly rate must be between 0 and 1000.");
        if (latitude is < -90m or > 90m)
            throw DomainException.BadRequest("invalid-facility", "The latitude must be between -90 and 90.");
        if (longitude is < -180m or > 180m)
            throw DomainException.BadRequest("invalid-facility", "The longitude must be between -180 and 180.");

        Name = Guard.RequiredText(name, 180, "invalid-facility", "name");
        Address = Guard.RequiredText(address, 300, "invalid-facility", "address");
        Latitude = latitude;
        Longitude = longitude;
        HourlyRate = decimal.Round(hourlyRate, 2, MidpointRounding.AwayFromZero);
        OpenTime = NormalizeTime(openTime, "00:00");
        CloseTime = NormalizeTime(closeTime, "23:59");
    }

    public void ChangeStatus(FacilityStatus status)
    {
        Status = status;
    }

    /// <summary>Great-circle distance in kilometres, or null when the facility has no coordinates.</summary>
    public double? DistanceInKilometresTo(double latitude, double longitude)
    {
        if (Latitude is null || Longitude is null) return null;
        const double earthRadiusKm = 6371.0;
        var lat1 = DegreesToRadians((double)Latitude.Value);
        var lat2 = DegreesToRadians(latitude);
        var deltaLat = lat2 - lat1;
        var deltaLon = DegreesToRadians(longitude - (double)Longitude.Value);
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static string NormalizeTime(string? value, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (candidate.Length > 5) candidate = candidate[..5];
        if (!TimeOnly.TryParseExact(candidate, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw DomainException.BadRequest("invalid-facility", "Opening and closing times must use the HH:mm format.");
        return candidate;
    }
}
