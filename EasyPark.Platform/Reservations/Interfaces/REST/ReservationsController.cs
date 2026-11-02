using System.Net.Mime;
using EasyPark.Platform.Reservations.Application.CommandServices;
using EasyPark.Platform.Reservations.Application.QueryServices;
using EasyPark.Platform.Reservations.Domain.Model.Commands;
using EasyPark.Platform.Reservations.Domain.Model.Queries;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Reservations.Interfaces.REST.Resources;
using EasyPark.Platform.Reservations.Interfaces.REST.Transform;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyPark.Platform.Reservations.Interfaces.REST;

/// <summary>Creation, query and cancellation of reservations.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Reservations")]
public class ReservationsController(
    IReservationCommandService reservationCommandService,
    IReservationQueryService reservationQueryService) : ControllerBase
{
    /// <summary>Drivers: their reservations. Operators: reservations of their facilities.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReservationResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReservations([FromQuery] int? driverProfileId, [FromQuery] int? vehicleId,
        [FromQuery] int? parkingFacilityId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = new GetReservationsQuery(User.GetUserAccountId(), User.IsOperator(), driverProfileId, vehicleId,
            parkingFacilityId, status);
        var reservations = await reservationQueryService.Handle(query, cancellationToken);
        return Ok(reservations.Select(ReservationResourceAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReservationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReservationById(int id, CancellationToken cancellationToken)
    {
        var reservation = await reservationQueryService.Handle(
                              new GetReservationByIdQuery(id, User.GetUserAccountId(), User.IsOperator()),
                              cancellationToken)
                          ?? throw DomainException.NotFound("reservation-not-found", "Reservation not found.");
        return Ok(ReservationResourceAssembler.ToResourceFromEntity(reservation));
    }

    /// <summary>Creates a CONFIRMED reservation and reserves the spot. 409 <c>no-availability</c> when there is no spot.</summary>
    [HttpPost]
    [Authorize(Roles = ClaimsPrincipalExtensions.DriverRole)]
    [ProducesResponseType(typeof(ReservationResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationResource resource,
        CancellationToken cancellationToken)
    {
        var command = ReservationResourceAssembler.ToCommandFromResource(resource, User.GetUserAccountId());
        var reservation = await reservationCommandService.Handle(command, cancellationToken);
        return Created($"/api/v1/reservations/{reservation.Id}",
            ReservationResourceAssembler.ToResourceFromEntity(reservation));
    }

    /// <summary>
    ///     Cancels the reservation when <c>status</c> is CANCELLED and frees the spot. Sending the current status
    ///     again is accepted (idempotent); any other change returns 409.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReservationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateReservation(int id, [FromBody] UpdateReservationResource resource,
        CancellationToken cancellationToken)
    {
        var requested = EnumCodes.Parse<ReservationStatus>(resource.Status, "invalid-status", "status");
        var current = await reservationQueryService.Handle(
                          new GetReservationByIdQuery(id, User.GetUserAccountId(), User.IsOperator()),
                          cancellationToken)
                      ?? throw DomainException.NotFound("reservation-not-found", "Reservation not found.");

        if (requested == current.Status) return Ok(ReservationResourceAssembler.ToResourceFromEntity(current));
        if (requested != ReservationStatus.Cancelled)
            throw DomainException.Conflict("status-managed-by-system",
                "Only cancellation can be requested; the other status changes are made by the platform.");

        var reservation = await reservationCommandService.Handle(
            new CancelReservationCommand(id, User.GetUserAccountId(), User.IsOperator()), cancellationToken);
        return Ok(ReservationResourceAssembler.ToResourceFromEntity(reservation));
    }

    /// <summary>Explicit cancellation endpoint (equivalent to PUT with status CANCELLED).</summary>
    [HttpPost("{id:int}/cancellation")]
    [ProducesResponseType(typeof(ReservationResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelReservation(int id, CancellationToken cancellationToken)
    {
        var reservation = await reservationCommandService.Handle(
            new CancelReservationCommand(id, User.GetUserAccountId(), User.IsOperator()), cancellationToken);
        return Ok(ReservationResourceAssembler.ToResourceFromEntity(reservation));
    }
}
