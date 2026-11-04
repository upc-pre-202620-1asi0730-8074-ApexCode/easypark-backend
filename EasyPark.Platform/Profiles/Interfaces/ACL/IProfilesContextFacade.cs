namespace EasyPark.Platform.Profiles.Interfaces.ACL;

/// <summary>Read model of a vehicle shared with other bounded contexts.</summary>
public record VehicleSnapshot(int Id, int? ProfileId, string PlateNumber, string VehicleType);

/// <summary>
///     Anti-corruption layer exposed by Profiles and Vehicles to the other bounded contexts.
/// </summary>
public interface IProfilesContextFacade
{
    Task<int?> FetchProfileIdByUserAccountIdAsync(int userAccountId, CancellationToken cancellationToken = default);

    Task<VehicleSnapshot?> FetchVehicleByIdAsync(int vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the vehicle of the plate, registering it without owner when it does not exist yet.
    ///     The new vehicle is saved immediately so that it gets an identifier.
    /// </summary>
    Task<VehicleSnapshot> FetchOrRegisterVehicleByPlateAsync(string plateNumber,
        CancellationToken cancellationToken = default);
}
