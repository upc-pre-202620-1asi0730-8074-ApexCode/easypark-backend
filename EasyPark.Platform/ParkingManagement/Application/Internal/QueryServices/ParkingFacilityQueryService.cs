using EasyPark.Platform.ParkingManagement.Application.QueryServices;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Queries;
using EasyPark.Platform.ParkingManagement.Domain.Repositories;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.ParkingManagement.Application.Internal.QueryServices;

public class ParkingFacilityQueryService(
    IParkingFacilityRepository facilityRepository,
    IParkingSpotRepository spotRepository,
    IProfilesContextFacade profilesContextFacade) : IParkingFacilityQueryService
{
    public async Task<IReadOnlyList<ParkingFacilityOverview>> Handle(SearchParkingFacilitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ParkingFacility> facilities;
        if (query.RequesterIsOperator)
        {
            var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(
                query.RequesterUserAccountId, cancellationToken);
            if (ownProfileId is null) return new List<ParkingFacilityOverview>();
            if (query.OperatorProfileId.HasValue && query.OperatorProfileId.Value != ownProfileId.Value)
                throw DomainException.Forbidden("forbidden", "An operator can only list their own facilities.");
            facilities = await facilityRepository.ListByOperatorProfileIdAsync(ownProfileId.Value, cancellationToken);
        }
        else
        {
            facilities = await facilityRepository.ListActiveAsync(cancellationToken);
            if (query.OperatorProfileId.HasValue)
                facilities = facilities.Where(facility => facility.IsManagedBy(query.OperatorProfileId.Value)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text.Trim();
            facilities = facilities.Where(facility =>
                facility.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                facility.Address.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (query.MaxHourlyRate.HasValue)
            facilities = facilities.Where(facility => facility.HourlyRate <= query.MaxHourlyRate.Value).ToList();

        var overviews = await BuildOverviewsAsync(facilities, query.Latitude, query.Longitude, cancellationToken);
        if (query.OnlyWithAvailability)
            overviews = overviews.Where(overview => overview.AvailableSpots > 0).ToList();

        return query.Latitude.HasValue && query.Longitude.HasValue
            ? overviews.OrderBy(overview => overview.DistanceKm ?? double.MaxValue).ToList()
            : overviews.OrderBy(overview => overview.Facility.Name).ToList();
    }

    public async Task<ParkingFacilityOverview?> Handle(GetParkingFacilityByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var facility = await facilityRepository.FindByIdAsync(query.FacilityId, cancellationToken);
        if (facility is null) return null;
        if (query.RequesterIsOperator)
        {
            var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(
                query.RequesterUserAccountId, cancellationToken);
            if (ownProfileId is null || !facility.IsManagedBy(ownProfileId.Value)) return null;
        }
        else if (!facility.IsActive)
        {
            return null;
        }

        var overviews = await BuildOverviewsAsync(new List<ParkingFacility> { facility }, null, null, cancellationToken);
        return overviews[0];
    }

    private async Task<List<ParkingFacilityOverview>> BuildOverviewsAsync(IReadOnlyList<ParkingFacility> facilities,
        double? latitude, double? longitude, CancellationToken cancellationToken)
    {
        if (facilities.Count == 0) return new List<ParkingFacilityOverview>();
        var ids = facilities.Select(facility => facility.Id).ToList();
        var spots = await spotRepository.ListByFacilityIdsAsync(ids, cancellationToken);
        return facilities.Select(facility =>
        {
            var facilitySpots = spots.Where(spot => spot.FacilityId == facility.Id).ToList();
            double? distance = latitude.HasValue && longitude.HasValue
                ? facility.DistanceInKilometresTo(latitude.Value, longitude.Value)
                : null;
            return new ParkingFacilityOverview(facility, facilitySpots.Count,
                facilitySpots.Count(spot => spot.IsAvailable), distance);
        }).ToList();
    }
}
