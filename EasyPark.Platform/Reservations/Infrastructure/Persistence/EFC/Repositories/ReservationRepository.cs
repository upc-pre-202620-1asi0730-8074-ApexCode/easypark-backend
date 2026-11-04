using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Reservations.Domain.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Reservations.Infrastructure.Persistence.EFC.Repositories;

public class ReservationRepository(AppDbContext context)
    : BaseRepository<Reservation>(context), IReservationRepository
{
    public async Task<IReadOnlyList<Reservation>> ListByDriverProfileIdAsync(int driverProfileId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reservation>()
            .Where(reservation => reservation.DriverProfileId == driverProfileId)
            .OrderByDescending(reservation => reservation.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default)
    {
        if (facilityIds.Count == 0) return new List<Reservation>();
        var ids = facilityIds.ToList();
        return await Context.Set<Reservation>()
            .Where(reservation => ids.Contains(reservation.ParkingFacilityId))
            .OrderByDescending(reservation => reservation.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ListOpenByVehicleIdAsync(int vehicleId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reservation>()
            .Where(reservation => reservation.VehicleId == vehicleId &&
                                  (reservation.Status == ReservationStatus.Pending ||
                                   reservation.Status == ReservationStatus.Confirmed ||
                                   reservation.Status == ReservationStatus.Active))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsPendingOrConfirmedBySpotIdAsync(int parkingSpotId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reservation>().AnyAsync(reservation =>
            reservation.ParkingSpotId == parkingSpotId &&
            (reservation.Status == ReservationStatus.Pending || reservation.Status == ReservationStatus.Confirmed),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ListConfirmedForEntryAsync(int vehicleId, int parkingFacilityId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reservation>()
            .Where(reservation => reservation.VehicleId == vehicleId &&
                                  reservation.ParkingFacilityId == parkingFacilityId &&
                                  (reservation.Status == ReservationStatus.Pending ||
                                   reservation.Status == ReservationStatus.Confirmed))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ListPendingOrConfirmedStartedBeforeAsync(DateTime instant,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Reservation>()
            .Where(reservation => reservation.StartAt < instant &&
                                  (reservation.Status == ReservationStatus.Pending ||
                                   reservation.Status == ReservationStatus.Confirmed))
            .ToListAsync(cancellationToken);
    }
}
