namespace EasyPark.Platform.ParkingManagement.Domain.Model.Commands;

public record CreateParkingFacilityCommand(
    int RequesterUserAccountId,
    int? OperatorProfileId,
    string? Name,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    decimal? HourlyRate,
    string? OpenTime,
    string? CloseTime,
    string? Status);

public record UpdateParkingFacilityCommand(
    int FacilityId,
    int RequesterUserAccountId,
    string? Name,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    decimal? HourlyRate,
    string? OpenTime,
    string? CloseTime,
    string? Status);

public record CreateParkingSpotCommand(
    int RequesterUserAccountId,
    int? FacilityId,
    string? Code,
    int? Level,
    string? SpotType);

public record UpdateParkingSpotCommand(
    int SpotId,
    int RequesterUserAccountId,
    string? Code,
    int? Level,
    string? SpotType,
    string? Status);