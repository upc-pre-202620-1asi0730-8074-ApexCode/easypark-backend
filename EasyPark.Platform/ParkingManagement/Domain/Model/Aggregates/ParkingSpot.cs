using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;

/// <summary>
///     Parking spot of a facility. Status transitions are guarded here; <see cref="Version" /> is an optimistic
///     concurrency token, so two requests cannot reserve or occupy the same spot at the same time.
/// </summary>
public class ParkingSpot
{
    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected ParkingSpot()
    {
        Code = string.Empty;
    }

    public ParkingSpot(int facilityId, string? code, int level, SpotType spotType)
    {
        if (facilityId <= 0) throw new ArgumentOutOfRangeException(nameof(facilityId));
        FacilityId = facilityId;
        Code = string.Empty;
        UpdateDetails(code, level, spotType);
        Status = SpotStatus.Available;
        Version = 1;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public int FacilityId { get; private set; }

    public string Code { get; private set; }

    public int Level { get; private set; }

    public SpotType SpotType { get; private set; }

    public SpotStatus Status { get; private set; }

    public int Version { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsAvailable => Status == SpotStatus.Available;

    public static string NormalizeCode(string? code)
    {
        return Guard.RequiredText(code, 30, "invalid-spot", "code").ToUpperInvariant();
    }

    public void UpdateDetails(string? code, int level, SpotType spotType)
    {
        if (level is < -10 or > 100)
            throw DomainException.BadRequest("invalid-spot", "The level must be between -10 and 100.");
        Code = NormalizeCode(code);
        Level = level;
        SpotType = spotType;
    }

    /// <summary>A reservation holds the spot.</summary>
    public void Reserve()
    {
        if (Status != SpotStatus.Available)
            throw DomainException.Conflict("no-availability", $"The spot {Code} is not available.");
        ChangeStatus(SpotStatus.Reserved);
    }

    /// <summary>A vehicle parks in the spot (from AVAILABLE or from its own reservation).</summary>
    public void Occupy()
    {
        if (Status is SpotStatus.Occupied or SpotStatus.OutOfService)
            throw DomainException.Conflict("no-availability", $"The spot {Code} cannot be occupied.");
        ChangeStatus(SpotStatus.Occupied);
    }

    /// <summary>The vehicle leaves or the reservation ends.</summary>
    public void Release()
    {
        if (Status is SpotStatus.Occupied or SpotStatus.Reserved) ChangeStatus(SpotStatus.Available);
    }

    public void MarkOutOfService()
    {
        ChangeStatus(SpotStatus.OutOfService);
    }

    public void ReturnToService()
    {
        if (Status != SpotStatus.OutOfService)
            throw DomainException.Conflict("invalid-spot-transition", "Only an out-of-service spot can return to service.");
        ChangeStatus(SpotStatus.Available);
    }

    private void ChangeStatus(SpotStatus status)
    {
        Status = status;
        Version++;
    }
}
