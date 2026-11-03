using System.Net.Mime;
using EasyPark.Platform.ParkingManagement.Application.CommandServices;
using EasyPark.Platform.ParkingManagement.Application.QueryServices;
using EasyPark.Platform.ParkingManagement.Domain.Model.Queries;
using EasyPark.Platform.ParkingManagement.Interfaces.REST.Resources;
using EasyPark.Platform.ParkingManagement.Interfaces.REST.Transform;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyPark.Platform.ParkingManagement.Interfaces.REST;

/// <summary>Parking spots of a facility and their status.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Parking Management")]
public class ParkingSpotsController(
    IParkingSpotCommandService spotCommandService,
    IParkingSpotQueryService spotQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParkingSpotResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpots([FromQuery] int? facilityId, CancellationToken cancellationToken)
    {
        var spots = await spotQueryService.Handle(
            new GetParkingSpotsQuery(facilityId, User.GetUserAccountId(), User.IsOperator()), cancellationToken);
        return Ok(spots.Select(ParkingSpotResourceAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ParkingSpotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpotById(int id, CancellationToken cancellationToken)
    {
        var spot = await spotQueryService.Handle(
                       new GetParkingSpotByIdQuery(id, User.GetUserAccountId(), User.IsOperator()), cancellationToken)
                   ?? throw DomainException.NotFound("spot-not-found", "Parking spot not found.");
        return Ok(ParkingSpotResourceAssembler.ToResourceFromEntity(spot));
    }

    [HttpPost]
    [Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles)]
    [ProducesResponseType(typeof(ParkingSpotResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSpot([FromBody] CreateParkingSpotResource resource,
        CancellationToken cancellationToken)
    {
        var command = ParkingSpotResourceAssembler.ToCommandFromResource(resource, User.GetUserAccountId());
        var spot = await spotCommandService.Handle(command, cancellationToken);
        return Created($"/api/v1/parking-spots/{spot.Id}", ParkingSpotResourceAssembler.ToResourceFromEntity(spot));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles)]
    [ProducesResponseType(typeof(ParkingSpotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSpot(int id, [FromBody] UpdateParkingSpotResource resource,
        CancellationToken cancellationToken)
    {
        var command = ParkingSpotResourceAssembler.ToCommandFromResource(id, resource, User.GetUserAccountId());
        var spot = await spotCommandService.Handle(command, cancellationToken);
        return Ok(ParkingSpotResourceAssembler.ToResourceFromEntity(spot));
    }
}
