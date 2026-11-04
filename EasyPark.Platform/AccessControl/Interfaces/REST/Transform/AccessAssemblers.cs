using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.Commands;
using EasyPark.Platform.AccessControl.Interfaces.REST.Resources;
using EasyPark.Platform.Shared.Domain.Model;

namespace EasyPark.Platform.AccessControl.Interfaces.REST.Transform;

public static class AccessMovementResourceAssembler
{
    public static AccessMovementResource ToResourceFromEntity(AccessMovement movement, int? parkingStayId = null)
    {
        return new AccessMovementResource(movement.Id, movement.ParkingFacilityId, movement.VehicleId,
            movement.PlateNumber, movement.OperatorId, EnumCodes.ToCode(movement.MovementType),
            EnumCodes.ToCode(movement.RegistrationMethod), EnumCodes.ToCode(movement.Status), movement.OccurredAt,
            movement.Note, parkingStayId);
    }

    public static RegisterAccessCommand ToCommandFromResource(RegisterAccessResource resource, int operatorId)
    {
        return new RegisterAccessCommand(operatorId, resource.ParkingFacilityId, resource.PlateNumber,
            resource.VehicleId, resource.MovementType, resource.RegistrationMethod, resource.ReservationCode);
    }
}

public static class ParkingStayResourceAssembler
{
    public static ParkingStayResource ToResourceFromEntity(ParkingStay stay)
    {
        return new ParkingStayResource(stay.Id, stay.ParkingFacilityId, stay.ParkingSpotId, stay.VehicleId,
            stay.ReservationId, stay.EntryMovementId, stay.ExitMovementId, stay.ChargedAmount, stay.Currency,
            stay.StartedAt, stay.EndedAt);
    }
}
