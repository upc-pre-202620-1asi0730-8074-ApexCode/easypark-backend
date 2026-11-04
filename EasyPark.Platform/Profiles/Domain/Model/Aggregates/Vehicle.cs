using EasyPark.Platform.Profiles.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Profiles.Domain.Model.Aggregates;

/// <summary>
///     Vehicle identified by its plate. A vehicle can exist without owner when its plate was read at a gate
///     before any driver registered it; a driver can later claim it.
/// </summary>
public class Vehicle
{
    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected Vehicle()
    {
        PlateNumber = string.Empty;
    }

    public Vehicle(PlateNumber plateNumber, int? profileId, VehicleType vehicleType, string? color)
    {
        PlateNumber = plateNumber.Value;
        ProfileId = profileId;
        VehicleType = vehicleType;
        Color = CleanColor(color);
        IsDefault = false;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public int? ProfileId { get; private set; }

    /// <summary>Normalized plate (validated through the <see cref="ValueObjects.PlateNumber" /> value object).</summary>
    public string PlateNumber { get; private set; }

    public VehicleType VehicleType { get; private set; }

    public string? Color { get; private set; }

    public bool IsDefault { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool HasOwner => ProfileId.HasValue;

    /// <summary>
    ///     Creates the vehicle of a plate read at a gate. The type is inferred from the plate format.
    /// </summary>
    public static Vehicle RegisterUnowned(PlateNumber plateNumber)
    {
        return new Vehicle(plateNumber, null,
            plateNumber.IsMotorcyclePlate ? VehicleType.Motorcycle : VehicleType.Car, null);
    }

    public bool IsOwnedBy(int profileId)
    {
        return ProfileId == profileId;
    }

    public void AssignOwner(int profileId)
    {
        if (ProfileId.HasValue && ProfileId.Value != profileId)
            throw DomainException.Conflict("plate-taken", "The plate number is already registered by another driver.");
        ProfileId = profileId;
    }

    public void ReleaseOwner()
    {
        ProfileId = null;
        IsDefault = false;
    }

    public void UpdateData(VehicleType vehicleType, string? color)
    {
        VehicleType = vehicleType;
        Color = CleanColor(color);
    }

    public void MarkAsDefault()
    {
        if (!HasOwner)
            throw DomainException.Conflict("vehicle-without-owner", "Only an owned vehicle can be the default one.");
        IsDefault = true;
    }

    public void UnmarkAsDefault()
    {
        IsDefault = false;
    }

    private static string? CleanColor(string? color)
    {
        return Guard.OptionalText(color, 60, "invalid-vehicle", "color");
    }
}
