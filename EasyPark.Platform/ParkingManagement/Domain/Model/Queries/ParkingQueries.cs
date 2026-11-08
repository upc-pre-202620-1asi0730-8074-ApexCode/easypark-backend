namespace EasyPark.Platform.ParkingManagement.Domain.Model.Queries;

/// <summary>
///     Facility search. Operators see their own facilities; drivers see ACTIVE facilities and can filter by
///     text, maximum hourly rate and availability, and sort by distance when coordinates are given.
/// </summary>
public record SearchParkingFacilitiesQuery(
    int RequesterUserAccountId,
    bool RequesterIsOperator,
    int? OperatorProfileId,
    string? Text,
    decimal? MaxHourlyRate,
    bool OnlyWithAvailability,
    double? Latitude,
    double? Longitude);

public record GetParkingFacilityByIdQuery(int FacilityId, int RequesterUserAccountId, bool RequesterIsOperator);

public record GetParkingSpotsQuery(int? FacilityId, int RequesterUserAccountId, bool RequesterIsOperator);

public record GetParkingSpotByIdQuery(int SpotId, int RequesterUserAccountId, bool RequesterIsOperator);
