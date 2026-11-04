namespace EasyPark.Platform.Reservations.Domain.Model.Commands;

/// <summary>
///     Creates a reservation. When <c>ParkingSpotId</c> is not provided the platform assigns the first available
///     spot of the facility that matches the vehicle type.
/// </summary>
public record CreateReservationCommand(
    int RequesterUserAccountId,
    int? DriverProfileId,
    int? VehicleId,
    int? ParkingFacilityId,
    int? ParkingSpotId,
    DateTime? StartAt,
    int? DurationMinutes);

public record CancelReservationCommand(int ReservationId, int RequesterUserAccountId, bool RequesterIsOperator);

public record ExpireOverdueReservationsCommand(DateTime Now, int GraceMinutes);
