using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.ParkingManagement.Domain.Repositories;
using EasyPark.Platform.ParkingManagement.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.ParkingManagement.Application.ACL;

public class ParkingManagementContextFacade(
    IParkingFacilityRepository facilityRepository,
    IParkingSpotRepository spotRepository) : IParkingManagementContextFacade
{
    public async Task<FacilitySnapshot?> FetchFacilityAsync(int facilityId, CancellationToken cancellationToken = default)
    {
        var facility = await facilityRepository.FindByIdAsync(facilityId, cancellationToken);
        return facility is null
            ? null
            : new FacilitySnapshot(facility.Id, facility.OperatorProfileId, facility.Name, facility.HourlyRate,
                facility.IsActive);
    }

    public async Task<IReadOnlyList<int>> FetchFacilityIdsByOperatorProfileIdAsync(int operatorProfileId,
        CancellationToken cancellationToken = default)
    {
        var facilities = await facilityRepository.ListByOperatorProfileIdAsync(operatorProfileId, cancellationToken);
        return facilities.Select(facility => facility.Id).ToList();
    }

    public async Task<SpotSnapshot?> FetchSpotAsync(int spotId, CancellationToken cancellationToken = default)
    {
        var spot = await spotRepository.FindByIdAsync(spotId, cancellationToken);
        return spot is null ? null : ToSnapshot(spot);
    }

    public async Task<SpotSnapshot?> FindAvailableSpotAsync(int facilityId, string spotTypeCode,
        CancellationToken cancellationToken = default)
    {
        if (!EnumCodes.TryParse<SpotType>(spotTypeCode, out var spotType)) return null;
        var spot = await spotRepository.FindFirstAvailableAsync(facilityId, spotType, cancellationToken);
        return spot is null ? null : ToSnapshot(spot);
    }

    public async Task ReserveSpotAsync(int spotId, CancellationToken cancellationToken = default)
    {
        var spot = await RequireSpotAsync(spotId, cancellationToken);
        spot.Reserve();
    }

    public async Task OccupySpotAsync(int spotId, CancellationToken cancellationToken = default)
    {
        var spot = await RequireSpotAsync(spotId, cancellationToken);
        spot.Occupy();
    }

    public async Task ReleaseSpotAsync(int spotId, CancellationToken cancellationToken = default)
    {
        var spot = await RequireSpotAsync(spotId, cancellationToken);
        spot.Release();
    }

    public async Task ReleaseReservedSpotAsync(int spotId, CancellationToken cancellationToken = default)
    {
        var spot = await spotRepository.FindByIdAsync(spotId, cancellationToken);
        if (spot is not null && spot.Status == SpotStatus.Reserved) spot.Release();
    }

    private async Task<ParkingSpot> RequireSpotAsync(int spotId, CancellationToken cancellationToken)
    {
        return await spotRepository.FindByIdAsync(spotId, cancellationToken)
               ?? throw DomainException.NotFound("spot-not-found", "Parking spot not found.");
    }

    private static SpotSnapshot ToSnapshot(ParkingSpot spot)
    {
        return new SpotSnapshot(spot.Id, spot.FacilityId, spot.Code, EnumCodes.ToCode(spot.SpotType),
            EnumCodes.ToCode(spot.Status));
    }
}
