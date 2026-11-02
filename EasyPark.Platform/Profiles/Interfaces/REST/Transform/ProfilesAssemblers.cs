using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Commands;
using EasyPark.Platform.Profiles.Interfaces.REST.Resources;
using EasyPark.Platform.Shared.Domain.Model;

namespace EasyPark.Platform.Profiles.Interfaces.REST.Transform;

public static class ProfileResourceAssembler
{
    public static ProfileResource ToResourceFromEntity(Profile profile)
    {
        return new ProfileResource(profile.Id, profile.UserAccountId, profile.FirstName, profile.LastName,
            profile.Phone, profile.PhotoUrl, profile.CompanyName, profile.JobTitle, profile.CreatedAt);
    }

    public static CreateProfileCommand ToCommandFromResource(CreateProfileResource resource, int requesterId,
        bool requesterIsOperator)
    {
        return new CreateProfileCommand(requesterId, requesterIsOperator, resource.UserAccountId, resource.FirstName,
            resource.LastName, resource.Phone, resource.PhotoUrl, resource.CompanyName, resource.JobTitle);
    }

    public static UpdateProfileCommand ToCommandFromResource(int profileId, UpdateProfileResource resource,
        int requesterId, bool requesterIsOperator)
    {
        return new UpdateProfileCommand(profileId, requesterId, requesterIsOperator, resource.FirstName,
            resource.LastName, resource.Phone, resource.PhotoUrl, resource.CompanyName, resource.JobTitle);
    }
}

public static class VehicleResourceAssembler
{
    public static VehicleResource ToResourceFromEntity(Vehicle vehicle)
    {
        return new VehicleResource(vehicle.Id, vehicle.ProfileId, vehicle.PlateNumber,
            EnumCodes.ToCode(vehicle.VehicleType), vehicle.Color, vehicle.IsDefault, vehicle.CreatedAt);
    }

    public static RegisterVehicleCommand ToCommandFromResource(CreateVehicleResource resource, int requesterId,
        bool requesterIsOperator)
    {
        return new RegisterVehicleCommand(requesterId, requesterIsOperator, resource.ProfileId, resource.PlateNumber,
            resource.VehicleType, resource.Color, resource.IsDefault);
    }

    public static UpdateVehicleCommand ToCommandFromResource(int vehicleId, UpdateVehicleResource resource,
        int requesterId, bool requesterIsOperator)
    {
        return new UpdateVehicleCommand(vehicleId, requesterId, requesterIsOperator, resource.ProfileId,
            resource.PlateNumber, resource.VehicleType, resource.Color, resource.IsDefault);
    }
}
