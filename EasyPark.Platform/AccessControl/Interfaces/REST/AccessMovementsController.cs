using System.Net.Mime;
using EasyPark.Platform.AccessControl.Application.CommandServices;
using EasyPark.Platform.AccessControl.Application.QueryServices;
using EasyPark.Platform.AccessControl.Domain.Model.Commands;
using EasyPark.Platform.AccessControl.Domain.Model.Queries;
using EasyPark.Platform.AccessControl.Interfaces.REST.Resources;
using EasyPark.Platform.AccessControl.Interfaces.REST.Transform;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyPark.Platform.AccessControl.Interfaces.REST;

/// <summary>Entries and exits registered at the gates of the operator's facilities.</summary>
[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles)]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Access Control")]
public class AccessMovementsController(
    IAccessControlCommandService accessControlCommandService,
    IAccessControlQueryService accessControlQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccessMovementResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovements([FromQuery] int? parkingFacilityId,
        CancellationToken cancellationToken)
    {
        var movements = await accessControlQueryService.Handle(
            new GetAccessMovementsQuery(User.GetUserAccountId(), parkingFacilityId), cancellationToken);
        return Ok(movements.Select(movement => AccessMovementResourceAssembler.ToResourceFromEntity(movement)));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AccessMovementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMovementById(int id, CancellationToken cancellationToken)
    {
        var movement = await accessControlQueryService.Handle(
                           new GetAccessMovementByIdQuery(id, User.GetUserAccountId()), cancellationToken)
                       ?? throw DomainException.NotFound("movement-not-found", "Access movement not found.");
        return Ok(AccessMovementResourceAssembler.ToResourceFromEntity(movement));
    }

    /// <summary>
    ///     Registers an entry or exit by plate. Always returns 201 with the stored movement; check
    ///     <c>status</c>: COMPLETED (allowed), REJECTED or UNDER_REVIEW (see <c>note</c>).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccessMovementResource), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterAccess([FromBody] RegisterAccessResource resource,
        CancellationToken cancellationToken)
    {
        var command = AccessMovementResourceAssembler.ToCommandFromResource(resource, User.GetUserAccountId());
        var result = await accessControlCommandService.Handle(command, cancellationToken);
        var body = AccessMovementResourceAssembler.ToResourceFromEntity(result.Movement, result.ParkingStay?.Id);
        return Created($"/api/v1/access-movements/{result.Movement.Id}", body);
    }

    /// <summary>Closes a movement UNDER_REVIEW with status REJECTED.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AccessMovementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMovement(int id, [FromBody] UpdateAccessMovementResource resource,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(resource.Status?.Trim(), "REJECTED", StringComparison.OrdinalIgnoreCase))
            throw DomainException.Conflict("status-managed-by-system",
                "A movement under review can only be closed with status REJECTED.");
        var movement = await accessControlCommandService.Handle(
            new RejectAccessMovementCommand(id, User.GetUserAccountId(), resource.Note), cancellationToken);
        return Ok(AccessMovementResourceAssembler.ToResourceFromEntity(movement));
    }
}
