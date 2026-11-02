using EasyPark.Platform.AccessControl.Interfaces.ACL;
using EasyPark.Platform.ParkingManagement.Application.CommandServices;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Commands;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.ParkingManagement.Domain.Repositories;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Reservations.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.ParkingManagement.Application.Internal.CommandServices;

/// <summary>
///     Spot use cases. Manual status changes made by the operator are validated against open parking stays
///     (Access Control) and pending reservations (Reservations) to keep the three contexts consistent.
/// </summary>
public class ParkingSpotCommandService(
    IParkingSpotRepository spotRepository,
    IParkingFacilityRepository facilityRepository,
    IProfilesContextFacade profilesContextFacade,
    IAccessControlContextFacade accessControlContextFacade,
    IReservationsContextFacade reservationsContextFacade,
    IUnitOfWork unitOfWork) : IParkingSpotCommandService
{
    public async Task<ParkingSpot> Handle(CreateParkingSpotCommand command, CancellationToken cancellationToken = default)
    {
        if (!command.FacilityId.HasValue)
            throw DomainException.BadRequest("invalid-spot", "The field 'facilityId' is required.");
        await RequireManagedFacilityAsync(command.FacilityId.Value, command.RequesterUserAccountId, cancellationToken);

        var code = ParkingSpot.NormalizeCode(command.Code);
        if (await spotRepository.ExistsCodeInFacilityAsync(command.FacilityId.Value, code, null, cancellationToken))
            throw DomainException.Conflict("code-taken", $"The code {code} already exists in this facility.");

        var spotType = string.IsNullOrWhiteSpace(command.SpotType)
            ? SpotType.Car
            : EnumCodes.Parse<SpotType>(command.SpotType, "invalid-spot", "spotType");
        var spot = new ParkingSpot(command.FacilityId.Value, code, command.Level ?? 1, spotType);
        await spotRepository.AddAsync(spot, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return spot;
    }

    public async Task<ParkingSpot> Handle(UpdateParkingSpotCommand command, CancellationToken cancellationToken = default)
    {
        var spot = await spotRepository.FindByIdAsync(command.SpotId, cancellationToken)
                   ?? throw DomainException.NotFound("spot-not-found", "Parking spot not found.");
        await RequireManagedFacilityAsync(spot.FacilityId, command.RequesterUserAccountId, cancellationToken);

        var code = string.IsNullOrWhiteSpace(command.Code) ? spot.Code : ParkingSpot.NormalizeCode(command.Code);
        if (await spotRepository.ExistsCodeInFacilityAsync(spot.FacilityId, code, spot.Id, cancellationToken))
            throw DomainException.Conflict("code-taken", $"The code {code} already exists in this facility.");
        var spotType = string.IsNullOrWhiteSpace(command.SpotType)
            ? spot.SpotType
            : EnumCodes.Parse<SpotType>(command.SpotType, "invalid-spot", "spotType");
        spot.UpdateDetails(code, command.Level ?? spot.Level, spotType);

        if (!string.IsNullOrWhiteSpace(command.Status))
        {
            var requested = EnumCodes.Parse<SpotStatus>(command.Status, "invalid-spot", "status");
            if (requested != spot.Status) await ApplyManualStatusAsync(spot, requested, cancellationToken);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return spot;
    }

    private async Task ApplyManualStatusAsync(ParkingSpot spot, SpotStatus requested,
        CancellationToken cancellationToken)
    {
        switch (requested)
        {
            case SpotStatus.Reserved:
                throw DomainException.Conflict("status-managed-by-system",
                    "A spot becomes RESERVED only through a reservation.");
            case SpotStatus.OutOfService:
                await EnsureNotInUseAsync(spot, cancellationToken);
                spot.MarkOutOfService();
                break;
            case SpotStatus.Available when spot.Status == SpotStatus.OutOfService:
                spot.ReturnToService();
                break;
            case SpotStatus.Available:
                await EnsureNotInUseAsync(spot, cancellationToken);
                spot.Release();
                break;
            case SpotStatus.Occupied:
                // Manual occupation (vehicle without reservation registered outside the system).
                if (spot.Status == SpotStatus.Reserved) await EnsureNotInUseAsync(spot, cancellationToken);
                spot.Occupy();
                break;
        }
    }

    private async Task EnsureNotInUseAsync(ParkingSpot spot, CancellationToken cancellationToken)
    {
        if (await accessControlContextFacade.HasOpenStayOnSpotAsync(spot.Id, cancellationToken))
            throw DomainException.Conflict("spot-in-use",
                "The spot has a vehicle registered by access control. Register its exit first.");
        if (await reservationsContextFacade.HasPendingReservationOnSpotAsync(spot.Id, cancellationToken))
            throw DomainException.Conflict("spot-in-use",
                "The spot is held by a confirmed reservation. The reservation must be cancelled first.");
    }

    private async Task<ParkingFacility> RequireManagedFacilityAsync(int facilityId, int userAccountId,
        CancellationToken cancellationToken)
    {
        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(userAccountId,
                               cancellationToken)
                           ?? throw DomainException.Unprocessable("profile-required",
                               "Create your operator profile before managing parking spots.");
        var facility = await facilityRepository.FindByIdAsync(facilityId, cancellationToken)
                       ?? throw DomainException.NotFound("facility-not-found", "Parking facility not found.");
        if (!facility.IsManagedBy(ownProfileId))
            throw DomainException.Forbidden("forbidden", "The parking facility belongs to another operator.");
        return facility;
    }
}
