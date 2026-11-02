using EasyPark.Platform.ParkingManagement.Application.CommandServices;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Commands;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.ParkingManagement.Domain.Repositories;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.ParkingManagement.Application.Internal.CommandServices;

public class ParkingFacilityCommandService(
    IParkingFacilityRepository facilityRepository,
    IProfilesContextFacade profilesContextFacade,
    IUnitOfWork unitOfWork) : IParkingFacilityCommandService
{
    public async Task<ParkingFacility> Handle(CreateParkingFacilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var ownProfileId = await RequireOperatorProfileIdAsync(command.RequesterUserAccountId, cancellationToken);
        if (command.OperatorProfileId.HasValue && command.OperatorProfileId.Value != ownProfileId)
            throw DomainException.Forbidden("forbidden", "operatorProfileId must be the profile of the authenticated operator.");
        if (command.HourlyRate is null)
            throw DomainException.BadRequest("invalid-facility", "The field 'hourlyRate' is required.");

        var status = string.IsNullOrWhiteSpace(command.Status)
            ? FacilityStatus.Active
            : EnumCodes.Parse<FacilityStatus>(command.Status, "invalid-facility", "status");
        var facility = new ParkingFacility(ownProfileId, command.Name, command.Address, command.Latitude,
            command.Longitude, command.HourlyRate.Value, command.OpenTime, command.CloseTime, status);
        await facilityRepository.AddAsync(facility, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return facility;
    }

    public async Task<ParkingFacility> Handle(UpdateParkingFacilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var ownProfileId = await RequireOperatorProfileIdAsync(command.RequesterUserAccountId, cancellationToken);
        var facility = await facilityRepository.FindByIdAsync(command.FacilityId, cancellationToken)
                       ?? throw DomainException.NotFound("facility-not-found", "Parking facility not found.");
        if (!facility.IsManagedBy(ownProfileId))
            throw DomainException.Forbidden("forbidden", "The parking facility belongs to another operator.");

        facility.UpdateDetails(command.Name, command.Address, command.Latitude, command.Longitude,
            command.HourlyRate ?? facility.HourlyRate, command.OpenTime, command.CloseTime);
        if (!string.IsNullOrWhiteSpace(command.Status))
            facility.ChangeStatus(EnumCodes.Parse<FacilityStatus>(command.Status, "invalid-facility", "status"));

        await unitOfWork.CompleteAsync(cancellationToken);
        return facility;
    }

    private async Task<int> RequireOperatorProfileIdAsync(int userAccountId, CancellationToken cancellationToken)
    {
        return await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(userAccountId, cancellationToken)
               ?? throw DomainException.Unprocessable("profile-required",
                   "Create your operator profile before managing parking facilities.");
    }
}
