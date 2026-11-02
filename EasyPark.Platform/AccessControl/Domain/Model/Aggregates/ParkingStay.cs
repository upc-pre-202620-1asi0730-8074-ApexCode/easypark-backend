using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.AccessControl.Domain.Model.Aggregates;

/// <summary>
///     Time a vehicle remains in a spot, from its entry movement to its exit movement.
///     The charged amount is calculated when the stay is closed.
/// </summary>
public class ParkingStay
{
    public const string DefaultCurrency = "PEN";

    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected ParkingStay()
    {
        Currency = DefaultCurrency;
    }

    public ParkingStay(int parkingFacilityId, int parkingSpotId, int vehicleId, int? reservationId,
        int entryMovementId, DateTime startedAt)
    {
        ParkingFacilityId = parkingFacilityId;
        ParkingSpotId = parkingSpotId;
        VehicleId = vehicleId;
        ReservationId = reservationId;
        EntryMovementId = entryMovementId;
        StartedAt = startedAt;
        Currency = DefaultCurrency;
    }

    public int Id { get; private set; }

    public int ParkingFacilityId { get; private set; }

    public int ParkingSpotId { get; private set; }

    public int VehicleId { get; private set; }

    public int? ReservationId { get; private set; }

    public int EntryMovementId { get; private set; }

    public int? ExitMovementId { get; private set; }

    public decimal? ChargedAmount { get; private set; }

    public string Currency { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? EndedAt { get; private set; }

    public bool IsOpen => ExitMovementId is null;

    public static decimal CalculateCharge(decimal hourlyRate, DateTime startedAt, DateTime endedAt)
    {
        var minutes = Math.Max(0, (int)Math.Floor((endedAt - startedAt).TotalMinutes));
        return decimal.Round(hourlyRate * minutes / 60m, 2, MidpointRounding.AwayFromZero);
    }

    public void Close(int exitMovementId, DateTime endedAt, decimal hourlyRate)
    {
        if (!IsOpen) throw DomainException.Conflict("stay-closed", "The parking stay is already closed.");
        ExitMovementId = exitMovementId;
        EndedAt = endedAt;
        ChargedAmount = CalculateCharge(hourlyRate, StartedAt, endedAt);
    }

    public void ReassignSpot(int parkingSpotId)
    {
        if (!IsOpen) throw DomainException.Conflict("stay-closed", "A closed parking stay cannot be reassigned.");
        ParkingSpotId = parkingSpotId;
    }
}
