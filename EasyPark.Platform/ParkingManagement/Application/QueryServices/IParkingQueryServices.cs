using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Queries;

namespace EasyPark.Platform.ParkingManagement.Application.QueryServices;

/// <summary>Facility together with its live availability.</summary>
public record ParkingFacilityOverview(ParkingFacility Facility, int TotalSpots, int AvailableSpots, double? DistanceKm);

public interface IParkingFacilityQueryService
{
    Task<IReadOnlyList<ParkingFacilityOverview>> Handle(SearchParkingFacilitiesQuery query,
        CancellationToken cancellationToken = default);

    Task<ParkingFacilityOverview?> Handle(GetParkingFacilityByIdQuery query,
        CancellationToken cancellationToken = default);
}

public interface IParkingSpotQueryService
{
    Task<IReadOnlyList<ParkingSpot>> Handle(GetParkingSpotsQuery query, CancellationToken cancellationToken = default);

    Task<ParkingSpot?> Handle(GetParkingSpotByIdQuery query, CancellationToken cancellationToken = default);
}
