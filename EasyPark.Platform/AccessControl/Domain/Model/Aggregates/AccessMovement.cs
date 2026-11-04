using EasyPark.Platform.AccessControl.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.AccessControl.Domain.Model.Aggregates;

/// <summary>
///     Entry or exit attempt registered at a facility gate. Rejected attempts are also stored (audit trail).
///     The plate read at the gate is kept as a snapshot.
/// </summary>
public class AccessMovement
{
    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected AccessMovement()
    {
        PlateNumber = string.Empty;
    }

    private AccessMovement(int parkingFacilityId, int vehicleId, string plateNumber, int operatorId,
        MovementType movementType, RegistrationMethod registrationMethod, MovementStatus status, string? note)
    {
        ParkingFacilityId = parkingFacilityId;
        VehicleId = vehicleId;
        PlateNumber = plateNumber;
        OperatorId = operatorId;
        MovementType = movementType;
        RegistrationMethod = registrationMethod;
        Status = status;
        Note = note;
        OccurredAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public int ParkingFacilityId { get; private set; }

    public int VehicleId { get; private set; }

    public string PlateNumber { get; private set; }

    /// <summary>User account id of the operator who registered the movement.</summary>
    public int OperatorId { get; private set; }

    public MovementType MovementType { get; private set; }

    public RegistrationMethod RegistrationMethod { get; private set; }

    public MovementStatus Status { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public string? Note { get; private set; }

    public static AccessMovement Completed(int parkingFacilityId, int vehicleId, string plateNumber, int operatorId,
        MovementType movementType, RegistrationMethod registrationMethod)
    {
        return new AccessMovement(parkingFacilityId, vehicleId, plateNumber, operatorId, movementType,
            registrationMethod, MovementStatus.Completed, null);
    }

    public static AccessMovement Rejected(int parkingFacilityId, int vehicleId, string plateNumber, int operatorId,
        MovementType movementType, RegistrationMethod registrationMethod, string reason)
    {
        return new AccessMovement(parkingFacilityId, vehicleId, plateNumber, operatorId, movementType,
            registrationMethod, MovementStatus.Rejected, reason);
    }

    public static AccessMovement UnderReview(int parkingFacilityId, int vehicleId, string plateNumber, int operatorId,
        MovementType movementType, RegistrationMethod registrationMethod, string reason)
    {
        return new AccessMovement(parkingFacilityId, vehicleId, plateNumber, operatorId, movementType,
            registrationMethod, MovementStatus.UnderReview, reason);
    }

    /// <summary>The operator closes a review by rejecting the movement.</summary>
    public void RejectReview(string? note)
    {
        if (Status != MovementStatus.UnderReview)
            throw DomainException.Conflict("invalid-movement-transition", "Only a movement under review can be rejected.");
        Status = MovementStatus.Rejected;
        Note = Guard.OptionalText(note, 200, "invalid-movement", "note") ?? Note;
    }
}
