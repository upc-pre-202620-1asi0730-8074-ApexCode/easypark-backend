using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Profiles.Domain.Repositories;

public interface IVehicleRepository : IBaseRepository<Vehicle>
{
    Task<Vehicle?> FindByPlateNumberAsync(string plateNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> ListByProfileIdAsync(int profileId, CancellationToken cancellationToken = default);
}
