using EasyPark.Platform.Profiles.Application.QueryServices;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Queries;
using EasyPark.Platform.Profiles.Domain.Model.ValueObjects;
using EasyPark.Platform.Profiles.Domain.Repositories;

namespace EasyPark.Platform.Profiles.Application.Internal.QueryServices;

public class VehicleQueryService(IVehicleRepository vehicleRepository) : IVehicleQueryService
{
    public async Task<IReadOnlyList<Vehicle>> Handle(GetVehiclesByProfileIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await vehicleRepository.ListByProfileIdAsync(query.ProfileId, cancellationToken);
    }

    public async Task<Vehicle?> Handle(GetVehicleByPlateNumberQuery query, CancellationToken cancellationToken = default)
    {
        if (!PlateNumber.TryCreate(query.PlateNumber, out var plateNumber)) return null;
        return await vehicleRepository.FindByPlateNumberAsync(plateNumber.Value, cancellationToken);
    }

    public async Task<Vehicle?> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await vehicleRepository.FindByIdAsync(query.VehicleId, cancellationToken);
    }
}
