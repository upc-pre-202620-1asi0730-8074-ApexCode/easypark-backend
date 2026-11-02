namespace EasyPark.Platform.Reservations.Domain.Model.Queries;

public record GetReservationsQuery(
    int RequesterUserAccountId,
    bool RequesterIsOperator,
    int? DriverProfileId,
    int? VehicleId,
    int? ParkingFacilityId,
    string? Status);

public record GetReservationByIdQuery(int ReservationId, int RequesterUserAccountId, bool RequesterIsOperator);
