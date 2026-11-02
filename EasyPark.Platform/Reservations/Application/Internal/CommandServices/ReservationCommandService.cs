using EasyPark.Platform.ParkingManagement.Interfaces.ACL;
using EasyPark.Platform.Profiles.Interfaces.ACL;
using EasyPark.Platform.Reservations.Application.CommandServices;
using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Commands;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Reservations.Domain.Repositories;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Reservations.Application.Internal.CommandServices;

/// <summary>
///     Reservation use cases. The spot is reserved in the same unit of work as the reservation and is protected by
///     an optimistic concurrency token, so the same spot cannot be booked twice.
/// </summary>
public class ReservationCommandService(
    IReservationRepository reservationRepository,
    IProfilesContextFacade profilesContextFacade,
    IParkingManagementContextFacade parkingContextFacade,
    IUnitOfWork unitOfWork,
    ILogger<ReservationCommandService> logger) : IReservationCommandService
{
    private static readonly TimeSpan StartTolerance = TimeSpan.FromMinutes(2);

    public async Task<Reservation> Handle(CreateReservationCommand command, CancellationToken cancellationToken = default)
    {
        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(
                               command.RequesterUserAccountId, cancellationToken)
                           ?? throw DomainException.Unprocessable("profile-required",
                               "Create your profile before booking a parking spot.");
        if (command.DriverProfileId.HasValue && command.DriverProfileId.Value != ownProfileId)
            throw DomainException.Forbidden("forbidden", "A driver can only reserve with their own profile.");
        if (!command.VehicleId.HasValue)
            throw DomainException.BadRequest("invalid-reservation", "The field 'vehicleId' is required.");
        if (!command.StartAt.HasValue)
            throw DomainException.BadRequest("invalid-start", "The field 'startAt' is required.");
        if (!command.DurationMinutes.HasValue)
            throw DomainException.BadRequest("invalid-duration", "The field 'durationMinutes' is required.");

        var startAt = Guard.ToUtc(command.StartAt.Value);
        if (startAt < DateTime.UtcNow - StartTolerance)
            throw DomainException.BadRequest("invalid-start", "The reservation cannot start in the past.");

        var vehicle = await profilesContextFacade.FetchVehicleByIdAsync(command.VehicleId.Value, cancellationToken);
        if (vehicle is null || vehicle.ProfileId != ownProfileId)
            throw DomainException.Unprocessable("vehicle-not-owned", "The selected vehicle is not registered on your profile.");

        var spot = await ResolveSpotAsync(command, vehicle.VehicleType, cancellationToken);
        var facility = await parkingContextFacade.FetchFacilityAsync(spot.FacilityId, cancellationToken)
                       ?? throw DomainException.NotFound("facility-not-found", "Parking facility not found.");
        if (!facility.IsActive)
            throw DomainException.Conflict("facility-not-active", "The parking facility is not accepting reservations.");
        if (!string.Equals(spot.SpotType, vehicle.VehicleType, StringComparison.Ordinal))
            throw DomainException.Unprocessable("spot-type-mismatch",
                $"The spot accepts {spot.SpotType} vehicles and the selected vehicle is {vehicle.VehicleType}.");

        var endAt = startAt.AddMinutes(command.DurationMinutes.Value);
        var openReservations = await reservationRepository.ListOpenByVehicleIdAsync(vehicle.Id, cancellationToken);
        if (openReservations.Any(item => item.Overlaps(startAt, endAt)))
            throw DomainException.Conflict("vehicle-already-reserved",
                "The vehicle already has a reservation that overlaps this time range.");

        var reservation = new Reservation(ReservationCode.Generate(), ownProfileId, vehicle.Id, facility.Id, spot.Id,
            startAt, command.DurationMinutes.Value, facility.HourlyRate);

        // Throws "no-availability" (409) when the spot is no longer AVAILABLE.
        await parkingContextFacade.ReserveSpotAsync(spot.Id, cancellationToken);
        await reservationRepository.AddAsync(reservation, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return reservation;
    }

    public async Task<Reservation> Handle(CancelReservationCommand command, CancellationToken cancellationToken = default)
    {
        var reservation = await reservationRepository.FindByIdAsync(command.ReservationId, cancellationToken)
                          ?? throw DomainException.NotFound("reservation-not-found", "Reservation not found.");
        await EnsureCanManageAsync(reservation, command.RequesterUserAccountId, command.RequesterIsOperator,
            cancellationToken);

        reservation.Cancel();
        await parkingContextFacade.ReleaseReservedSpotAsync(reservation.ParkingSpotId, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return reservation;
    }

    public async Task<int> Handle(ExpireOverdueReservationsCommand command, CancellationToken cancellationToken = default)
    {
        var candidates = await reservationRepository.ListPendingOrConfirmedStartedBeforeAsync(command.Now,
            cancellationToken);
        var expired = 0;
        foreach (var reservation in candidates.Where(item => item.IsOverdue(command.Now, command.GraceMinutes)))
        {
            reservation.Expire();
            await parkingContextFacade.ReleaseReservedSpotAsync(reservation.ParkingSpotId, cancellationToken);
            expired++;
        }

        if (expired == 0) return 0;
        await unitOfWork.CompleteAsync(cancellationToken);
        logger.LogInformation("{Count} overdue reservation(s) expired", expired);
        return expired;
    }

    private async Task<SpotSnapshot> ResolveSpotAsync(CreateReservationCommand command, string vehicleType,
        CancellationToken cancellationToken)
    {
        if (command.ParkingSpotId.HasValue)
        {
            var spot = await parkingContextFacade.FetchSpotAsync(command.ParkingSpotId.Value, cancellationToken)
                       ?? throw DomainException.NotFound("spot-not-found", "Parking spot not found.");
            if (command.ParkingFacilityId.HasValue && command.ParkingFacilityId.Value != spot.FacilityId)
                throw DomainException.BadRequest("invalid-reservation", "The spot does not belong to the facility.");
            if (!string.Equals(spot.Status, "AVAILABLE", StringComparison.Ordinal))
                throw DomainException.Conflict("no-availability", "The selected spot is not available.");
            return spot;
        }

        if (!command.ParkingFacilityId.HasValue)
            throw DomainException.BadRequest("invalid-reservation", "Provide parkingFacilityId or parkingSpotId.");

        return await parkingContextFacade.FindAvailableSpotAsync(command.ParkingFacilityId.Value, vehicleType,
                   cancellationToken)
               ?? throw DomainException.Conflict("no-availability",
                   "There are no available spots for this vehicle type in the facility.");
    }

    private async Task EnsureCanManageAsync(Reservation reservation, int userAccountId, bool isOperator,
        CancellationToken cancellationToken)
    {
        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(userAccountId,
            cancellationToken);
        if (ownProfileId is null)
            throw DomainException.Forbidden("forbidden", "You cannot manage this reservation.");

        if (!isOperator)
        {
            if (reservation.DriverProfileId != ownProfileId.Value)
                throw DomainException.Forbidden("forbidden", "A driver can only manage their own reservations.");
            return;
        }

        var facility = await parkingContextFacade.FetchFacilityAsync(reservation.ParkingFacilityId, cancellationToken);
        if (facility is null || facility.OperatorProfileId != ownProfileId.Value)
            throw DomainException.Forbidden("forbidden", "The reservation belongs to another operator's facility.");
    }
}
