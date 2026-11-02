namespace EasyPark.Platform.Reservations.Interfaces.ACL;

/// <summary>Read model of a reservation shared with other bounded contexts.</summary>
public record ReservationSnapshot(int Id, string Code, int VehicleId, int ParkingFacilityId, int ParkingSpotId,
    string Status);

/// <summary>Anti-corruption layer exposed by Reservations to the other bounded contexts.</summary>
public interface IReservationsContextFacade
{
    /// <summary>Whether a PENDING or CONFIRMED reservation holds the spot.</summary>
    Task<bool> HasPendingReservationOnSpotAsync(int parkingSpotId, CancellationToken cancellationToken = default);

    /// <summary>The CONFIRMED reservation that allows the vehicle to enter the facility, if any.</summary>
    Task<ReservationSnapshot?> FindReservationForEntryAsync(int vehicleId, int parkingFacilityId,
        string? reservationCode, CancellationToken cancellationToken = default);

    /// <summary>Marks the reservation ACTIVE (the vehicle entered). The caller saves the unit of work.</summary>
    Task ActivateReservationAsync(int reservationId, CancellationToken cancellationToken = default);

    /// <summary>Marks the reservation COMPLETED (the vehicle left). The caller saves the unit of work.</summary>
    Task CompleteReservationAsync(int reservationId, CancellationToken cancellationToken = default);
}
