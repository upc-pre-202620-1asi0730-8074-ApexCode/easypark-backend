using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.AccessControl.Domain.Repositories;

public interface IAccessMovementRepository : IBaseRepository<AccessMovement>
{
    Task<IReadOnlyList<AccessMovement>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default);
}

public interface IParkingStayRepository : IBaseRepository<ParkingStay>
{
    Task<ParkingStay?> FindOpenByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default);

    Task<bool> ExistsOpenBySpotIdAsync(int parkingSpotId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingStay>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default);
}
