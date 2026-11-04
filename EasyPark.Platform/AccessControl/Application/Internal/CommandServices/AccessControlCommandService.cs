using EasyPark.Platform.AccessControl.Application.CommandServices;
using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.Commands;
using EasyPark.Platform.AccessControl.Domain.Model.ValueObjects;
using EasyPark.Platform.AccessControl.Domain.Repositories;
using EasyPark.Platform.ParkingManagement.Interfaces.ACL;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Reservations.Interfaces.ACL;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.AccessControl.Application.Internal.CommandServices;

/// <summary>
///     Gate use cases. Entry: requires an owned vehicle without an open stay and a CONFIRMED reservation in the
///     facility; opens a stay, occupies the spot and activates the reservation. Exit: closes the open stay, charges
///     the time, frees the spot and completes the reservation. Everything runs in one database transaction.
/// </summary>
public class AccessControlCommandService(
    IAccessMovementRepository movementRepository,
    IParkingStayRepository stayRepository,
    IProfilesContextFacade profilesContextFacade,
    IParkingManagementContextFacade parkingContextFacade,
    IReservationsContextFacade reservationsContextFacade,
    IUnitOfWork unitOfWork) : IAccessControlCommandService
{
    public async Task<AccessRegistrationResult> Handle(RegisterAccessCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.ParkingFacilityId.HasValue)
            throw DomainException.BadRequest("invalid-access", "The field 'parkingFacilityId' is required.");
        var facility = await RequireManagedFacilityAsync(command.ParkingFacilityId.Value,
            command.OperatorUserAccountId, cancellationToken);
        var movementType = EnumCodes.Parse<MovementType>(command.MovementType, "invalid-access", "movementType");
        var method = string.IsNullOrWhiteSpace(command.RegistrationMethod)
            ? RegistrationMethod.Manual
            : EnumCodes.Parse<RegistrationMethod>(command.RegistrationMethod, "invalid-access", "registrationMethod");

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var vehicle = await ResolveVehicleAsync(command, cancellationToken);
            var context = new GateContext(facility, vehicle, command.OperatorUserAccountId, movementType, method);

            if (!vehicle.ProfileId.HasValue)
                return await RecordAsync(context, MovementStatus.UnderReview, AccessRejectionReasons.VehicleUnderReview,
                    cancellationToken);

            return movementType == MovementType.Entry
                ? await RegisterEntryAsync(context, command.ReservationCode, cancellationToken)
                : await RegisterExitAsync(context, cancellationToken);
        }, cancellationToken);
    }

    public async Task<AccessMovement> Handle(RejectAccessMovementCommand command,
        CancellationToken cancellationToken = default)
    {
        var movement = await movementRepository.FindByIdAsync(command.MovementId, cancellationToken)
                       ?? throw DomainException.NotFound("movement-not-found", "Access movement not found.");
        await RequireManagedFacilityAsync(movement.ParkingFacilityId, command.OperatorUserAccountId, cancellationToken);
        movement.RejectReview(command.Note);
        await unitOfWork.CompleteAsync(cancellationToken);
        return movement;
    }

    public async Task<ParkingStay> Handle(ReassignParkingSpotCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.ParkingSpotId.HasValue)
            throw DomainException.BadRequest("invalid-space", "The field 'parkingSpotId' is required.");
        var stay = await stayRepository.FindByIdAsync(command.ParkingStayId, cancellationToken)
                   ?? throw DomainException.NotFound("stay-not-found", "Parking stay not found.");
        await RequireManagedFacilityAsync(stay.ParkingFacilityId, command.OperatorUserAccountId, cancellationToken);
        if (stay.ParkingSpotId == command.ParkingSpotId.Value) return stay;
        if (!stay.IsOpen) throw DomainException.Conflict("stay-closed", "A closed parking stay cannot be reassigned.");

        var newSpot = await parkingContextFacade.FetchSpotAsync(command.ParkingSpotId.Value, cancellationToken);
        if (newSpot is null || newSpot.FacilityId != stay.ParkingFacilityId || newSpot.Status != "AVAILABLE")
            throw DomainException.Conflict("invalid-space", "The new spot must be AVAILABLE and in the same facility.");

        await parkingContextFacade.ReleaseSpotAsync(stay.ParkingSpotId, cancellationToken);
        await parkingContextFacade.OccupySpotAsync(newSpot.Id, cancellationToken);
        stay.ReassignSpot(newSpot.Id);
        await unitOfWork.CompleteAsync(cancellationToken);
        return stay;
    }

    private async Task<AccessRegistrationResult> RegisterEntryAsync(GateContext context, string? reservationCode,
        CancellationToken cancellationToken)
    {
        if (await stayRepository.FindOpenByVehicleIdAsync(context.Vehicle.Id, cancellationToken) is not null)
            return await RecordAsync(context, MovementStatus.Rejected, AccessRejectionReasons.VehicleAlreadyInside,
                cancellationToken);

        var reservation = await reservationsContextFacade.FindReservationForEntryAsync(context.Vehicle.Id,
            context.Facility.Id, reservationCode, cancellationToken);
        if (reservation is null)
            return await RecordAsync(context, MovementStatus.Rejected, AccessRejectionReasons.NoActiveReservation,
                cancellationToken);

        var spot = await SelectSpotForEntryAsync(context, reservation, cancellationToken);
        if (spot is null)
            return await RecordAsync(context, MovementStatus.Rejected, AccessRejectionReasons.NoAvailability,
                cancellationToken);

        var movement = AccessMovement.Completed(context.Facility.Id, context.Vehicle.Id, context.Vehicle.PlateNumber,
            context.OperatorUserAccountId, MovementType.Entry, context.Method);
        await movementRepository.AddAsync(movement, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        var stay = new ParkingStay(context.Facility.Id, spot.Id, context.Vehicle.Id, reservation.Id, movement.Id,
            movement.OccurredAt);
        await stayRepository.AddAsync(stay, cancellationToken);
        if (spot.Id != reservation.ParkingSpotId)
            await parkingContextFacade.ReleaseReservedSpotAsync(reservation.ParkingSpotId, cancellationToken);
        await parkingContextFacade.OccupySpotAsync(spot.Id, cancellationToken);
        await reservationsContextFacade.ActivateReservationAsync(reservation.Id, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return new AccessRegistrationResult(movement, stay);
    }

    private async Task<AccessRegistrationResult> RegisterExitAsync(GateContext context,
        CancellationToken cancellationToken)
    {
        var stay = await stayRepository.FindOpenByVehicleIdAsync(context.Vehicle.Id, cancellationToken);
        if (stay is null || stay.ParkingFacilityId != context.Facility.Id)
            return await RecordAsync(context, MovementStatus.Rejected, AccessRejectionReasons.NoActiveStay,
                cancellationToken);

        var movement = AccessMovement.Completed(context.Facility.Id, context.Vehicle.Id, context.Vehicle.PlateNumber,
            context.OperatorUserAccountId, MovementType.Exit, context.Method);
        await movementRepository.AddAsync(movement, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        stay.Close(movement.Id, movement.OccurredAt, context.Facility.HourlyRate);
        await parkingContextFacade.ReleaseSpotAsync(stay.ParkingSpotId, cancellationToken);
        if (stay.ReservationId.HasValue)
            await reservationsContextFacade.CompleteReservationAsync(stay.ReservationId.Value, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return new AccessRegistrationResult(movement, stay);
    }

    /// <summary>The reserved spot when it can still be used; otherwise the first AVAILABLE spot of the vehicle type.</summary>
    private async Task<SpotSnapshot?> SelectSpotForEntryAsync(GateContext context, ReservationSnapshot reservation,
        CancellationToken cancellationToken)
    {
        var reserved = await parkingContextFacade.FetchSpotAsync(reservation.ParkingSpotId, cancellationToken);
        if (reserved is not null && reserved.FacilityId == context.Facility.Id &&
            reserved.Status is "RESERVED" or "AVAILABLE")
            return reserved;
        return await parkingContextFacade.FindAvailableSpotAsync(context.Facility.Id, context.Vehicle.VehicleType,
            cancellationToken);
    }

    private async Task<AccessRegistrationResult> RecordAsync(GateContext context, MovementStatus status, string reason,
        CancellationToken cancellationToken)
    {
        var movement = status == MovementStatus.UnderReview
            ? AccessMovement.UnderReview(context.Facility.Id, context.Vehicle.Id, context.Vehicle.PlateNumber,
                context.OperatorUserAccountId, context.MovementType, context.Method, reason)
            : AccessMovement.Rejected(context.Facility.Id, context.Vehicle.Id, context.Vehicle.PlateNumber,
                context.OperatorUserAccountId, context.MovementType, context.Method, reason);
        await movementRepository.AddAsync(movement, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return new AccessRegistrationResult(movement, null);
    }

    private async Task<VehicleSnapshot> ResolveVehicleAsync(RegisterAccessCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.PlateNumber))
            return await profilesContextFacade.FetchOrRegisterVehicleByPlateAsync(command.PlateNumber,
                cancellationToken);
        if (command.VehicleId.HasValue)
            return await profilesContextFacade.FetchVehicleByIdAsync(command.VehicleId.Value, cancellationToken)
                   ?? throw DomainException.NotFound("vehicle-not-found", "Vehicle not found.");
        throw DomainException.BadRequest("invalid-plate-number", "The field 'plateNumber' is required.");
    }

    private async Task<FacilitySnapshot> RequireManagedFacilityAsync(int facilityId, int operatorUserAccountId,
        CancellationToken cancellationToken)
    {
        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(operatorUserAccountId,
                               cancellationToken)
                           ?? throw DomainException.Unprocessable("profile-required",
                               "Create your operator profile before using access control.");
        var facility = await parkingContextFacade.FetchFacilityAsync(facilityId, cancellationToken)
                       ?? throw DomainException.NotFound("facility-not-found", "Parking facility not found.");
        if (facility.OperatorProfileId != ownProfileId)
            throw DomainException.Forbidden("forbidden", "The parking facility belongs to another operator.");
        return facility;
    }

    private sealed record GateContext(
        FacilitySnapshot Facility,
        VehicleSnapshot Vehicle,
        int OperatorUserAccountId,
        MovementType MovementType,
        RegistrationMethod Method);
}
