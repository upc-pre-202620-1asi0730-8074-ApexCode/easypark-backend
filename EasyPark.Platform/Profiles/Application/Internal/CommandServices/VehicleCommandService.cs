using EasyPark.Platform.Profiles.Application.CommandServices;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Commands;
using EasyPark.Platform.Profiles.Domain.Model.ValueObjects;
using EasyPark.Platform.Profiles.Domain.Repositories;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Profiles.Application.Internal.CommandServices;

/// <summary>
///     Vehicle use cases. A driver registers vehicles on their own profile (or claims an unowned plate);
///     operators can only create unowned vehicles (plates read at the gate).
/// </summary>
public class VehicleCommandService(
    IVehicleRepository vehicleRepository,
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork) : IVehicleCommandService
{
    public async Task<Vehicle> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken = default)
    {
        var plateNumber = PlateNumber.Create(command.PlateNumber);
        var existing = await vehicleRepository.FindByPlateNumberAsync(plateNumber.Value, cancellationToken);

        if (command.RequesterIsOperator)
        {
            if (command.ProfileId.HasValue)
                throw DomainException.Forbidden("forbidden", "Operators can only register vehicles without owner.");
            if (existing is not null)
                throw DomainException.Conflict("plate-taken", "The plate number is already registered.");
            var unowned = Vehicle.RegisterUnowned(plateNumber);
            await vehicleRepository.AddAsync(unowned, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return unowned;
        }

        var ownProfileId = await RequireOwnProfileIdAsync(command.RequesterUserAccountId, cancellationToken);
        if (command.ProfileId.HasValue && command.ProfileId.Value != ownProfileId)
            throw DomainException.Forbidden("forbidden", "A driver can only register vehicles on their own profile.");

        var vehicleType = EnumCodes.Parse<VehicleType>(command.VehicleType, "invalid-vehicle-type", "vehicleType");
        var ownedVehicles = await vehicleRepository.ListByProfileIdAsync(ownProfileId, cancellationToken);

        Vehicle vehicle;
        if (existing is null)
        {
            vehicle = new Vehicle(plateNumber, ownProfileId, vehicleType, command.Color);
            await vehicleRepository.AddAsync(vehicle, cancellationToken);
        }
        else if (!existing.HasOwner)
        {
            // The plate was read at a gate before the driver registered it: the driver claims it.
            vehicle = existing;
            vehicle.AssignOwner(ownProfileId);
            vehicle.UpdateData(vehicleType, command.Color);
        }
        else if (existing.IsOwnedBy(ownProfileId))
        {
            throw DomainException.Conflict("plate-already-yours", "The vehicle is already registered on your profile.");
        }
        else
        {
            throw DomainException.Conflict("plate-taken", "The plate number is already registered by another driver.");
        }

        var otherVehicles = ownedVehicles.Where(item => item.Id != vehicle.Id).ToList();
        if (command.IsDefault || otherVehicles.Count == 0)
        {
            foreach (var other in otherVehicles) other.UnmarkAsDefault();
            vehicle.MarkAsDefault();
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return vehicle;
    }

    public async Task<Vehicle> Handle(UpdateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        if (command.RequesterIsOperator)
            throw DomainException.Forbidden("forbidden", "Only the driver can update their vehicles.");

        var vehicle = await vehicleRepository.FindByIdAsync(command.VehicleId, cancellationToken)
                      ?? throw DomainException.NotFound("vehicle-not-found", "Vehicle not found.");
        var ownProfileId = await RequireOwnProfileIdAsync(command.RequesterUserAccountId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.PlateNumber) &&
            PlateNumber.Create(command.PlateNumber).Value != vehicle.PlateNumber)
            throw DomainException.BadRequest("plate-immutable",
                "The plate number of a vehicle cannot be changed. Register a new vehicle instead.");

        if (vehicle.HasOwner && !vehicle.IsOwnedBy(ownProfileId))
            throw DomainException.Forbidden("forbidden", "The vehicle belongs to another driver.");

        if (command.ProfileId.HasValue && command.ProfileId.Value != ownProfileId)
            throw DomainException.Forbidden("forbidden", "A driver can only assign vehicles to their own profile.");

        if (!command.ProfileId.HasValue)
        {
            // PUT is a full update: a null profileId removes the vehicle from the driver's profile.
            if (!vehicle.HasOwner)
                throw DomainException.Forbidden("forbidden", "Assign the vehicle to your profile to claim it.");
            vehicle.ReleaseOwner();
            await unitOfWork.CompleteAsync(cancellationToken);
            return vehicle;
        }

        if (!vehicle.HasOwner) vehicle.AssignOwner(ownProfileId);

        var vehicleType = string.IsNullOrWhiteSpace(command.VehicleType)
            ? vehicle.VehicleType
            : EnumCodes.Parse<VehicleType>(command.VehicleType, "invalid-vehicle-type", "vehicleType");
        vehicle.UpdateData(vehicleType, command.Color);

        if (command.IsDefault)
        {
            var ownedVehicles = await vehicleRepository.ListByProfileIdAsync(ownProfileId, cancellationToken);
            foreach (var other in ownedVehicles.Where(item => item.Id != vehicle.Id)) other.UnmarkAsDefault();
            vehicle.MarkAsDefault();
        }
        else
        {
            vehicle.UnmarkAsDefault();
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return vehicle;
    }

    private async Task<int> RequireOwnProfileIdAsync(int userAccountId, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserAccountIdAsync(userAccountId, cancellationToken);
        return profile?.Id ?? throw DomainException.Unprocessable("profile-required",
            "Create your profile before registering vehicles.");
    }
}
