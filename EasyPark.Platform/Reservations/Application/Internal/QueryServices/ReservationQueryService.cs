using EasyPark.Platform.ParkingManagement.Interfaces.ACL;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Reservations.Application.QueryServices;
using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Queries;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Reservations.Domain.Repositories;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Reservations.Application.Internal.QueryServices;

public class ReservationQueryService(
    IReservationRepository reservationRepository,
    IProfilesContextFacade profilesContextFacade,
    IParkingManagementContextFacade parkingContextFacade) : IReservationQueryService
{
    public async Task<IReadOnlyList<Reservation>> Handle(GetReservationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(
            query.RequesterUserAccountId, cancellationToken);
        if (ownProfileId is null) return new List<Reservation>();

        IEnumerable<Reservation> reservations;
        if (query.RequesterIsOperator)
        {
            var facilityIds = await parkingContextFacade.FetchFacilityIdsByOperatorProfileIdAsync(ownProfileId.Value,
                cancellationToken);
            reservations = await reservationRepository.ListByFacilityIdsAsync(facilityIds.ToList(), cancellationToken);
            if (query.DriverProfileId.HasValue)
                reservations = reservations.Where(item => item.DriverProfileId == query.DriverProfileId.Value);
        }
        else
        {
            if (query.DriverProfileId.HasValue && query.DriverProfileId.Value != ownProfileId.Value)
                throw DomainException.Forbidden("forbidden", "A driver can only read their own reservations.");
            reservations = await reservationRepository.ListByDriverProfileIdAsync(ownProfileId.Value, cancellationToken);
        }

        if (query.VehicleId.HasValue)
            reservations = reservations.Where(item => item.VehicleId == query.VehicleId.Value);
        if (query.ParkingFacilityId.HasValue)
            reservations = reservations.Where(item => item.ParkingFacilityId == query.ParkingFacilityId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = EnumCodes.Parse<ReservationStatus>(query.Status, "invalid-status", "status");
            reservations = reservations.Where(item => item.Status == status);
        }

        return reservations.OrderByDescending(item => item.StartAt).ToList();
    }

    public async Task<Reservation?> Handle(GetReservationByIdQuery query, CancellationToken cancellationToken = default)
    {
        var reservation = await reservationRepository.FindByIdAsync(query.ReservationId, cancellationToken);
        if (reservation is null) return null;
        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(
            query.RequesterUserAccountId, cancellationToken);
        if (ownProfileId is null) return null;
        if (!query.RequesterIsOperator) return reservation.DriverProfileId == ownProfileId.Value ? reservation : null;

        var facility = await parkingContextFacade.FetchFacilityAsync(reservation.ParkingFacilityId, cancellationToken);
        return facility is not null && facility.OperatorProfileId == ownProfileId.Value ? reservation : null;
    }
}
