namespace EasyPark.Platform.Reservations.Interfaces.REST.Resources;

public record ReservationResource(
    int Id,
    string Code,
    int DriverProfileId,
    int VehicleId,
    int ParkingFacilityId,
    int ParkingSpotId,
    DateTime StartAt,
    int DurationMinutes,
    DateTime EndAt,
    decimal EstimatedAmount,
    string Currency,
    string Status,
    DateTime CreatedAt);

/// <summary>
///     Body of POST /api/v1/reservations. Send <c>parkingFacilityId</c> to let the platform assign a compatible spot,
///     or <c>parkingSpotId</c> to request a specific one. Amount, code and status are calculated by the server.
/// </summary>
public record CreateReservationResource(
    int? DriverProfileId,
    int? VehicleId,
    int? ParkingFacilityId,
    int? ParkingSpotId,
    DateTime? StartAt,
    int? DurationMinutes);

/// <summary>
///     Body of PUT /api/v1/reservations/{id}. The only status change a client can request is CANCELLED; the
///     other transitions are performed by access control and the expiration process.
/// </summary>
public record UpdateReservationResource(string? Status);
