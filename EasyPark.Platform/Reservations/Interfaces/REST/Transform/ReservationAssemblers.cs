using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Commands;
using EasyPark.Platform.Reservations.Interfaces.REST.Resources;
using EasyPark.Platform.Shared.Domain.Model;

namespace EasyPark.Platform.Reservations.Interfaces.REST.Transform;

public static class ReservationResourceAssembler
{
    public static ReservationResource ToResourceFromEntity(Reservation reservation)
    {
        return new ReservationResource(reservation.Id, reservation.Code, reservation.DriverProfileId,
            reservation.VehicleId, reservation.ParkingFacilityId, reservation.ParkingSpotId, reservation.StartAt,
            reservation.DurationMinutes, reservation.EndAt, reservation.EstimatedAmount, reservation.Currency,
            EnumCodes.ToCode(reservation.Status), reservation.CreatedAt);
    }

    public static CreateReservationCommand ToCommandFromResource(CreateReservationResource resource, int requesterId)
    {
        return new CreateReservationCommand(requesterId, resource.DriverProfileId, resource.VehicleId,
            resource.ParkingFacilityId, resource.ParkingSpotId, resource.StartAt, resource.DurationMinutes);
    }
}
