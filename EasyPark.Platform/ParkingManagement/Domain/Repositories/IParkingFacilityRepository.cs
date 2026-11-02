using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.ParkingManagement.Domain.Repositories;

public interface IParkingFacilityRepository : IBaseRepository<ParkingFacility>
{
    Task<IReadOnlyList<ParkingFacility>> ListByOperatorProfileIdAsync(int operatorProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingFacility>> ListActiveAsync(CancellationToken cancellationToken = default);
}