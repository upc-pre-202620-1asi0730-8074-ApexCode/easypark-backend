namespace EasyPark.Platform.AccessControl.Interfaces.REST.Resources;

/// <summary>
///     Access movement. When <c>status</c> is REJECTED or UNDER_REVIEW, <c>note</c> contains the reason code
///     (vehicle-under-review, vehicle-already-inside, no-active-reservation, no-availability, no-active-stay).
/// </summary>
public record AccessMovementResource(
    int Id,
    int ParkingFacilityId,
    int VehicleId,
    string PlateNumber,
    int OperatorId,
    string MovementType,
    string RegistrationMethod,
    string Status,
    DateTime OccurredAt,
    string? Note,
    int? ParkingStayId);

/// <summary>Body of POST /api/v1/access-movements (register an entry or an exit at the gate).</summary>
public record RegisterAccessResource(
    int? ParkingFacilityId,
    string? PlateNumber,
    int? VehicleId,
    string? MovementType,
    string? RegistrationMethod,
    string? ReservationCode);

/// <summary>Body of PUT /api/v1/access-movements/{id}: closes a review with status REJECTED.</summary>
public record UpdateAccessMovementResource(string? Status, string? Note);

public record ParkingStayResource(
    int Id,
    int ParkingFacilityId,
    int ParkingSpotId,
    int VehicleId,
    int? ReservationId,
    int EntryMovementId,
    int? ExitMovementId,
    decimal? ChargedAmount,
    string Currency,
    DateTime StartedAt,
    DateTime? EndedAt);

/// <summary>Body of PUT /api/v1/parking-stays/{id}: moves an open stay to another AVAILABLE spot.</summary>
public record UpdateParkingStayResource(int? ParkingSpotId);
