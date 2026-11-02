namespace EasyPark.Platform.Profiles.Interfaces.REST.Resources;

public record ProfileResource(
    int Id,
    int UserAccountId,
    string FirstName,
    string LastName,
    string Phone,
    string? PhotoUrl,
    string? CompanyName,
    string? JobTitle,
    DateTime CreatedAt);

/// <summary>Body of POST /api/v1/profiles. Extra fields sent by the Web Application (id, createdAt) are ignored.</summary>
public record CreateProfileResource(
    int? UserAccountId,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? PhotoUrl,
    string? CompanyName,
    string? JobTitle);

/// <summary>Body of PUT /api/v1/profiles/{id}.</summary>
public record UpdateProfileResource(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? PhotoUrl,
    string? CompanyName,
    string? JobTitle);

public record VehicleResource(
    int Id,
    int? ProfileId,
    string PlateNumber,
    string VehicleType,
    string? Color,
    bool IsDefault,
    DateTime CreatedAt);

/// <summary>Body of POST /api/v1/vehicles.</summary>
public record CreateVehicleResource(
    int? ProfileId,
    string? PlateNumber,
    string? VehicleType,
    string? Color,
    bool IsDefault);

/// <summary>Body of PUT /api/v1/vehicles/{id}. A null profileId removes the vehicle from the driver's profile.</summary>
public record UpdateVehicleResource(
    int? ProfileId,
    string? PlateNumber,
    string? VehicleType,
    string? Color,
    bool IsDefault);
