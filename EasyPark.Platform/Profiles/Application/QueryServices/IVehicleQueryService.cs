using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Queries;

namespace EasyPark.Platform.Profiles.Application.QueryServices;

public interface IVehicleQueryService
{
    Task<IReadOnlyList<Vehicle>> Handle(GetVehiclesByProfileIdQuery query, CancellationToken cancellationToken = default);

    Task<Vehicle?> Handle(GetVehicleByPlateNumberQuery query, CancellationToken cancellationToken = default);

    Task<Vehicle?> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken = default);
}
