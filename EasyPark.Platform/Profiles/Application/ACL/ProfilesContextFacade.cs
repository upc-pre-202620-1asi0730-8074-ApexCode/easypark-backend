using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.ValueObjects;
using EasyPark.Platform.Profiles.Domain.Repositories;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Profiles.Application.ACL;

public class ProfilesContextFacade(
    IProfileRepository profileRepository,
    IVehicleRepository vehicleRepository,
    IUnitOfWork unitOfWork) : IProfilesContextFacade
{
    public async Task<int?> FetchProfileIdByUserAccountIdAsync(int userAccountId,
        CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.FindByUserAccountIdAsync(userAccountId, cancellationToken);
        return profile?.Id;
    }

    public async Task<VehicleSnapshot?> FetchVehicleByIdAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await vehicleRepository.FindByIdAsync(vehicleId, cancellationToken);
        return vehicle is null ? null : ToSnapshot(vehicle);
    }

    public async Task<VehicleSnapshot> FetchOrRegisterVehicleByPlateAsync(string plateNumber,
        CancellationToken cancellationToken = default)
    {
        var plate = PlateNumber.Create(plateNumber);
        var vehicle = await vehicleRepository.FindByPlateNumberAsync(plate.Value, cancellationToken);
        if (vehicle is not null) return ToSnapshot(vehicle);

        vehicle = Vehicle.RegisterUnowned(plate);
        await vehicleRepository.AddAsync(vehicle, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return ToSnapshot(vehicle);
    }

    private static VehicleSnapshot ToSnapshot(Vehicle vehicle)
    {
        return new VehicleSnapshot(vehicle.Id, vehicle.ProfileId, vehicle.PlateNumber,
            EnumCodes.ToCode(vehicle.VehicleType));
    }
}
