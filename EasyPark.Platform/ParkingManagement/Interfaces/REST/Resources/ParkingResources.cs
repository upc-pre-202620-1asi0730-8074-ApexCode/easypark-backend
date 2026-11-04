namespace EasyPark.Platform.ParkingManagement.Interfaces.REST.Resources;

/// <summary>Parking facility. <c>totalSpots</c> and <c>availableSpots</c> are calculated live.</summary>
public record ParkingFacilityResource(
    int Id,
    int OperatorProfileId,
    string Name,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    decimal HourlyRate,
    string OpenTime,
    string CloseTime,
    string Status,
    DateTime CreatedAt,
    int TotalSpots,
    int AvailableSpots,
    double? DistanceKm);

public record CreateParkingFacilityResource(
    int? OperatorProfileId,
    string? Name,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    decimal? HourlyRate,
    string? OpenTime,
    string? CloseTime,
    string? Status);

public record UpdateParkingFacilityResource(
    string? Name,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    decimal? HourlyRate,
    string? OpenTime,
    string? CloseTime,
    string? Status);

public record ParkingSpotResource(
    int Id,
    int FacilityId,
    string Code,
    int Level,
    string SpotType,
    string Status,
    DateTime CreatedAt);

public record CreateParkingSpotResource(int? FacilityId, string? Code, int? Level, string? SpotType);

/// <summary>
///     Body of PUT /api/v1/parking-spots/{id}. <c>status</c> accepts manual operator changes
///     (AVAILABLE, OCCUPIED, OUT_OF_SERVICE); RESERVED is managed by reservations.
/// </summary>
public record UpdateParkingSpotResource(string? Code, int? Level, string? SpotType, string? Status);