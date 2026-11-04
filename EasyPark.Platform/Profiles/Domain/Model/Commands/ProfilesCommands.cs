namespace EasyPark.Platform.Profiles.Domain.Model.Commands;

public record CreateProfileCommand(
    int RequesterUserAccountId,
    bool RequesterIsOperator,
    int? UserAccountId,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? PhotoUrl,
    string? CompanyName,
    string? JobTitle);

public record UpdateProfileCommand(
    int ProfileId,
    int RequesterUserAccountId,
    bool RequesterIsOperator,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? PhotoUrl,
    string? CompanyName,
    string? JobTitle);

public record RegisterVehicleCommand(
    int RequesterUserAccountId,
    bool RequesterIsOperator,
    int? ProfileId,
    string? PlateNumber,
    string? VehicleType,
    string? Color,
    bool IsDefault);

public record UpdateVehicleCommand(
    int VehicleId,
    int RequesterUserAccountId,
    bool RequesterIsOperator,
    int? ProfileId,
    string? PlateNumber,
    string? VehicleType,
    string? Color,
    bool IsDefault);
