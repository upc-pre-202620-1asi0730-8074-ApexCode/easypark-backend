namespace EasyPark.Platform.ParkingManagement.Interfaces.ACL;

public record FacilitySnapshot(int Id, int OperatorProfileId, string Name, decimal HourlyRate, bool IsActive);

public record SpotSnapshot(int Id, int FacilityId, string Code, string SpotType, string Status);

/// <summary>
///     Anti-corruption layer exposed by Parking Management. Status-changing methods only modify the tracked
///     spot; the calling use case saves the unit of work so the whole operation is atomic.
/// </summary>
public interface IParkingManagementContextFacade
{
    Task<FacilitySnapshot?> FetchFacilityAsync(int facilityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> FetchFacilityIdsByOperatorProfileIdAsync(int operatorProfileId,
        CancellationToken cancellationToken = default);

    Task<SpotSnapshot?> FetchSpotAsync(int spotId, CancellationToken cancellationToken = default);

    Task<SpotSnapshot?> FindAvailableSpotAsync(int facilityId, string spotTypeCode,
        CancellationToken cancellationToken = default);

    Task ReserveSpotAsync(int spotId, CancellationToken cancellationToken = default);

    Task OccupySpotAsync(int spotId, CancellationToken cancellationToken = default);

    /// <summary>Frees an OCCUPIED or RESERVED spot.</summary>
    Task ReleaseSpotAsync(int spotId, CancellationToken cancellationToken = default);

    /// <summary>Frees the spot only when it is RESERVED.</summary>
    Task ReleaseReservedSpotAsync(int spotId, CancellationToken cancellationToken = default);
}
