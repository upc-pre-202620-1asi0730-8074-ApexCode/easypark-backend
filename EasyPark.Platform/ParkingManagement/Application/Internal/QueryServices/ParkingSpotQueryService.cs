using EasyPark.Platform.ParkingManagement.Application.QueryServices;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Queries;
using EasyPark.Platform.ParkingManagement.Domain.Repositories;
using EasyPark.Platform.Profiles.Interfaces.ACL;

namespace EasyPark.Platform.ParkingManagement.Application.Internal.QueryServices;

public class ParkingSpotQueryService(
    IParkingSpotRepository spotRepository,
    IParkingFacilityRepository facilityRepository,
    IProfilesContextFacade profilesContextFacade) : IParkingSpotQueryService
{
    public async Task<IReadOnlyList<ParkingSpot>> Handle(GetParkingSpotsQuery query,
        CancellationToken cancellationToken = default)
    {
        var visibleFacilityIds = await VisibleFacilityIdsAsync(query.RequesterUserAccountId,
            query.RequesterIsOperator, cancellationToken);

        if (query.FacilityId.HasValue)
        {
            if (!visibleFacilityIds.Contains(query.FacilityId.Value)) return new List<ParkingSpot>();
            return await spotRepository.ListByFacilityIdAsync(query.FacilityId.Value, cancellationToken);
        }

        return await spotRepository.ListByFacilityIdsAsync(visibleFacilityIds, cancellationToken);
    }

    public async Task<ParkingSpot?> Handle(GetParkingSpotByIdQuery query, CancellationToken cancellationToken = default)
    {
        var spot = await spotRepository.FindByIdAsync(query.SpotId, cancellationToken);
        if (spot is null) return null;
        var visibleFacilityIds = await VisibleFacilityIdsAsync(query.RequesterUserAccountId,
            query.RequesterIsOperator, cancellationToken);
        return visibleFacilityIds.Contains(spot.FacilityId) ? spot : null;
    }

    /// <summary>Operators see the spots of their facilities; drivers see the spots of ACTIVE facilities.</summary>
    private async Task<List<int>> VisibleFacilityIdsAsync(int userAccountId, bool isOperator,
        CancellationToken cancellationToken)
    {
        if (!isOperator)
        {
            var active = await facilityRepository.ListActiveAsync(cancellationToken);
            return active.Select(facility => facility.Id).ToList();
        }

        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(userAccountId,
            cancellationToken);
        if (ownProfileId is null) return new List<int>();
        var own = await facilityRepository.ListByOperatorProfileIdAsync(ownProfileId.Value, cancellationToken);
        return own.Select(facility => facility.Id).ToList();
    }
}
