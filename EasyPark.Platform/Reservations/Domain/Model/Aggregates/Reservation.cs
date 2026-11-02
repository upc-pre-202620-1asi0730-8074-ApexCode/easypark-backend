using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Reservations.Domain.Model.Aggregates;

/// <summary>
///     Reservation of a parking spot by a driver for a vehicle and a time window.
///     Life cycle: CONFIRMED → ACTIVE (vehicle entered) → COMPLETED (vehicle left);
///     CONFIRMED → CANCELLED (driver) or EXPIRED (window passed without entry).
/// </summary>
public class Reservation
{
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 1440;
    public const string DefaultCurrency = "PEN";

    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected Reservation()
    {
        Code = string.Empty;
        Currency = DefaultCurrency;
    }

    public Reservation(string code, int driverProfileId, int vehicleId, int parkingFacilityId, int parkingSpotId,
        DateTime startAt, int durationMinutes, decimal hourlyRate)
    {
        if (durationMinutes is < MinDurationMinutes or > MaxDurationMinutes)
            throw DomainException.BadRequest("invalid-duration",
                $"The duration must be between {MinDurationMinutes} and {MaxDurationMinutes} minutes.");
        Code = code;
        DriverProfileId = driverProfileId;
        VehicleId = vehicleId;
        ParkingFacilityId = parkingFacilityId;
        ParkingSpotId = parkingSpotId;
        StartAt = Guard.ToUtc(startAt);
        DurationMinutes = durationMinutes;
        EstimatedAmount = CalculateAmount(hourlyRate, durationMinutes);
        Currency = DefaultCurrency;
        Status = ReservationStatus.Confirmed;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string Code { get; private set; }

    public int DriverProfileId { get; private set; }

    public int VehicleId { get; private set; }

    public int ParkingFacilityId { get; private set; }

    public int ParkingSpotId { get; private set; }

    public DateTime StartAt { get; private set; }

    public int DurationMinutes { get; private set; }

    public decimal EstimatedAmount { get; private set; }

    public string Currency { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime EndAt => StartAt.AddMinutes(DurationMinutes);

    /// <summary>PENDING, CONFIRMED or ACTIVE: the reservation still holds or uses a spot.</summary>
    public bool IsOpen => Status is ReservationStatus.Pending or ReservationStatus.Confirmed or ReservationStatus.Active;

    public static decimal CalculateAmount(decimal hourlyRate, int minutes)
    {
        return decimal.Round(hourlyRate * minutes / 60m, 2, MidpointRounding.AwayFromZero);
    }

    public bool Overlaps(DateTime startAt, DateTime endAt)
    {
        return StartAt < endAt && startAt < EndAt;
    }

    public bool IsOverdue(DateTime now, int graceMinutes)
    {
        return Status is ReservationStatus.Pending or ReservationStatus.Confirmed &&
               EndAt.AddMinutes(graceMinutes) < now;
    }

    public void Activate()
    {
        if (Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw DomainException.Conflict("invalid-reservation-transition",
                $"A {EnumCodes.ToCode(Status)} reservation cannot become ACTIVE.");
        Status = ReservationStatus.Active;
    }

    public void Complete()
    {
        if (Status != ReservationStatus.Active)
            throw DomainException.Conflict("invalid-reservation-transition",
                $"A {EnumCodes.ToCode(Status)} reservation cannot be completed.");
        Status = ReservationStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw DomainException.Conflict("not-cancellable",
                $"A {EnumCodes.ToCode(Status)} reservation cannot be cancelled.");
        Status = ReservationStatus.Cancelled;
    }

    public void Expire()
    {
        if (Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw DomainException.Conflict("invalid-reservation-transition",
                $"A {EnumCodes.ToCode(Status)} reservation cannot expire.");
        Status = ReservationStatus.Expired;
    }
}
