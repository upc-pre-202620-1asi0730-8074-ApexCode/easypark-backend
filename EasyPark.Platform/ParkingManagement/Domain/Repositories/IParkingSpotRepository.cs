using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.ParkingManagement.Domain.Repositories;

public interface IParkingSpotRepository : IBaseRepository<ParkingSpot>
{
    Task<IReadOnlyList<ParkingSpot>> ListByFacilityIdAsync(int facilityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpot>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsCodeInFacilityAsync(int facilityId, string code, int? excludedSpotId,
        CancellationToken cancellationToken = default);

    Task<ParkingSpot?> FindFirstAvailableAsync(int facilityId, SpotType spotType,
        CancellationToken cancellationToken = default);
}
