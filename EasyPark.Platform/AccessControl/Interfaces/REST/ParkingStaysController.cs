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

/// <summary>Parking stays (vehicles inside the operator's facilities).</summary>
[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles)]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Access Control")]
public class ParkingStaysController(
    IAccessControlCommandService accessControlCommandService,
    IAccessControlQueryService accessControlQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParkingStayResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStays([FromQuery] int? parkingFacilityId, [FromQuery] bool? open,
        CancellationToken cancellationToken)
    {
        var stays = await accessControlQueryService.Handle(
            new GetParkingStaysQuery(User.GetUserAccountId(), parkingFacilityId, open ?? false), cancellationToken);
        return Ok(stays.Select(ParkingStayResourceAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ParkingStayResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStayById(int id, CancellationToken cancellationToken)
    {
        var stay = await accessControlQueryService.Handle(new GetParkingStayByIdQuery(id, User.GetUserAccountId()),
                       cancellationToken)
                   ?? throw DomainException.NotFound("stay-not-found", "Parking stay not found.");
        return Ok(ParkingStayResourceAssembler.ToResourceFromEntity(stay));
    }

    /// <summary>Reassigns an open stay to another AVAILABLE spot of the same facility.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ParkingStayResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStay(int id, [FromBody] UpdateParkingStayResource resource,
        CancellationToken cancellationToken)
    {
        var stay = await accessControlCommandService.Handle(
            new ReassignParkingSpotCommand(id, User.GetUserAccountId(), resource.ParkingSpotId), cancellationToken);
        return Ok(ParkingStayResourceAssembler.ToResourceFromEntity(stay));
    }
}
