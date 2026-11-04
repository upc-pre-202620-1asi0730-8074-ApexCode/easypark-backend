namespace EasyPark.Platform.Reservations.Domain.Model.ValueObjects;

/// <summary>Contract codes: PENDING, CONFIRMED, ACTIVE, COMPLETED, CANCELLED, EXPIRED.</summary>
public enum ReservationStatus
{
    Pending,
    Confirmed,
    Active,
    Completed,
    Cancelled,
    Expired
}
