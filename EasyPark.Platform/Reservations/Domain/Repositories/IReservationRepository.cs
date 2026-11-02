using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Reservations.Domain.Repositories;

public interface IReservationRepository : IBaseRepository<Reservation>
{
    Task<IReadOnlyList<Reservation>> ListByDriverProfileIdAsync(int driverProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> ListOpenByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default);

    Task<bool> ExistsPendingOrConfirmedBySpotIdAsync(int parkingSpotId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> ListConfirmedForEntryAsync(int vehicleId, int parkingFacilityId,
        CancellationToken cancellationToken = default);

    /// <summary>PENDING or CONFIRMED reservations that started before the given instant.</summary>
    Task<IReadOnlyList<Reservation>> ListPendingOrConfirmedStartedBeforeAsync(DateTime instant,
        CancellationToken cancellationToken = default);
}
