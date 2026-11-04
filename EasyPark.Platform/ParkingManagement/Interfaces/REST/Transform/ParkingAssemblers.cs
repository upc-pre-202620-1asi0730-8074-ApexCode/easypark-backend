using EasyPark.Platform.ParkingManagement.Application.QueryServices;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Commands;
using EasyPark.Platform.ParkingManagement.Interfaces.REST.Resources;
using EasyPark.Platform.Shared.Domain.Model;

namespace EasyPark.Platform.ParkingManagement.Interfaces.REST.Transform;

public static class ParkingFacilityResourceAssembler
{
    public static ParkingFacilityResource ToResourceFromOverview(ParkingFacilityOverview overview)
    {
        var facility = overview.Facility;
        return new ParkingFacilityResource(facility.Id, facility.OperatorProfileId, facility.Name, facility.Address,
            facility.Latitude, facility.Longitude, facility.HourlyRate, facility.OpenTime, facility.CloseTime,
            EnumCodes.ToCode(facility.Status), facility.CreatedAt, overview.TotalSpots, overview.AvailableSpots,
            overview.DistanceKm.HasValue ? Math.Round(overview.DistanceKm.Value, 2) : (double?)null);
    }

    public static CreateParkingFacilityCommand ToCommandFromResource(CreateParkingFacilityResource resource,
        int requesterId)
    {
        return new CreateParkingFacilityCommand(requesterId, resource.OperatorProfileId, resource.Name,
            resource.Address, resource.Latitude, resource.Longitude, resource.HourlyRate, resource.OpenTime,
            resource.CloseTime, resource.Status);
    }

    public static UpdateParkingFacilityCommand ToCommandFromResource(int facilityId,
        UpdateParkingFacilityResource resource, int requesterId)
    {
        return new UpdateParkingFacilityCommand(facilityId, requesterId, resource.Name, resource.Address,
            resource.Latitude, resource.Longitude, resource.HourlyRate, resource.OpenTime, resource.CloseTime,
            resource.Status);
    }
}

public static class ParkingSpotResourceAssembler
{
    public static ParkingSpotResource ToResourceFromEntity(ParkingSpot spot)
    {
        return new ParkingSpotResource(spot.Id, spot.FacilityId, spot.Code, spot.Level,
            EnumCodes.ToCode(spot.SpotType), EnumCodes.ToCode(spot.Status), spot.CreatedAt);
    }

    public static CreateParkingSpotCommand ToCommandFromResource(CreateParkingSpotResource resource, int requesterId)
    {
        return new CreateParkingSpotCommand(requesterId, resource.FacilityId, resource.Code, resource.Level,
            resource.SpotType);
    }

    public static UpdateParkingSpotCommand ToCommandFromResource(int spotId, UpdateParkingSpotResource resource,
        int requesterId)
    {
        return new UpdateParkingSpotCommand(spotId, requesterId, resource.Code, resource.Level, resource.SpotType,
            resource.Status);
    }
}
