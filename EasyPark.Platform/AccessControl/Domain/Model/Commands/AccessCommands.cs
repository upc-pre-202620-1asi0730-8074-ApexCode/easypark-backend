namespace EasyPark.Platform.AccessControl.Domain.Model.Commands;

/// <summary>
///     Registers an entry or exit at a facility. The vehicle is identified by its plate (or id). The outcome is
///     a movement COMPLETED, REJECTED or UNDER_REVIEW; the rules are applied by the server in one transaction.
/// </summary>
public record RegisterAccessCommand(
    int OperatorUserAccountId,
    int? ParkingFacilityId,
    string? PlateNumber,
    int? VehicleId,
    string? MovementType,
    string? RegistrationMethod,
    string? ReservationCode);

public record RejectAccessMovementCommand(int MovementId, int OperatorUserAccountId, string? Note);

public record ReassignParkingSpotCommand(int ParkingStayId, int OperatorUserAccountId, int? ParkingSpotId);
