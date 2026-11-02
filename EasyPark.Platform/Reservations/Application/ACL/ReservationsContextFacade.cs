using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Reservations.Domain.Repositories;
using EasyPark.Platform.Reservations.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Model;

namespace EasyPark.Platform.Reservations.Application.ACL;

public class ReservationsContextFacade(IReservationRepository reservationRepository) : IReservationsContextFacade
{
    public async Task<bool> HasPendingReservationOnSpotAsync(int parkingSpotId,
        CancellationToken cancellationToken = default)
    {
        return await reservationRepository.ExistsPendingOrConfirmedBySpotIdAsync(parkingSpotId, cancellationToken);
    }

    public async Task<ReservationSnapshot?> FindReservationForEntryAsync(int vehicleId, int parkingFacilityId,
        string? reservationCode, CancellationToken cancellationToken = default)
    {
        var candidates = await reservationRepository.ListConfirmedForEntryAsync(vehicleId, parkingFacilityId,
            cancellationToken);
        var code = ReservationCode.Normalize(reservationCode);
        var reservation = candidates
            .Where(item => code is null || item.Code == code)
            .OrderBy(item => item.StartAt)
            .FirstOrDefault();
        return reservation is null ? null : ToSnapshot(reservation);
    }

    public async Task ActivateReservationAsync(int reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservationRepository.FindByIdAsync(reservationId, cancellationToken);
        reservation?.Activate();
    }

    public async Task CompleteReservationAsync(int reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await reservationRepository.FindByIdAsync(reservationId, cancellationToken);
        if (reservation is not null && reservation.Status == ReservationStatus.Active) reservation.Complete();
    }

    private static ReservationSnapshot ToSnapshot(Reservation reservation)
    {
        return new ReservationSnapshot(reservation.Id, reservation.Code, reservation.VehicleId,
            reservation.ParkingFacilityId, reservation.ParkingSpotId, EnumCodes.ToCode(reservation.Status));
    }
}
