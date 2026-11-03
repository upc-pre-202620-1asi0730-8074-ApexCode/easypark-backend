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

/// <summary>Parking facilities: search, detail, rate, location and management.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Parking Management")]
public class ParkingFacilitiesController(
    IParkingFacilityCommandService facilityCommandService,
    IParkingFacilityQueryService facilityQueryService) : ControllerBase
{
    /// <summary>
    ///     Operators: their facilities. Drivers: ACTIVE facilities. Optional filters: <c>q</c> (name/address),
    ///     <c>maxHourlyRate</c>, <c>onlyAvailable=true</c>, and <c>latitude</c>+<c>longitude</c> to sort by distance.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParkingFacilityResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacilities([FromQuery] int? operatorProfileId, [FromQuery] string? q,
        [FromQuery] decimal? maxHourlyRate, [FromQuery] bool? onlyAvailable, [FromQuery] double? latitude,
        [FromQuery] double? longitude, CancellationToken cancellationToken)
    {
        var query = new SearchParkingFacilitiesQuery(User.GetUserAccountId(), User.IsOperator(), operatorProfileId, q,
            maxHourlyRate, onlyAvailable ?? false, latitude, longitude);
        var overviews = await facilityQueryService.Handle(query, cancellationToken);
        return Ok(overviews.Select(ParkingFacilityResourceAssembler.ToResourceFromOverview));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ParkingFacilityResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFacilityById(int id, CancellationToken cancellationToken)
    {
        var overview = await facilityQueryService.Handle(
                           new GetParkingFacilityByIdQuery(id, User.GetUserAccountId(), User.IsOperator()),
                           cancellationToken)
                       ?? throw DomainException.NotFound("facility-not-found", "Parking facility not found.");
        return Ok(ParkingFacilityResourceAssembler.ToResourceFromOverview(overview));
    }

    [HttpPost]
    [Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles)]
    [ProducesResponseType(typeof(ParkingFacilityResource), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFacility([FromBody] CreateParkingFacilityResource resource,
        CancellationToken cancellationToken)
    {
        var command = ParkingFacilityResourceAssembler.ToCommandFromResource(resource, User.GetUserAccountId());
        var facility = await facilityCommandService.Handle(command, cancellationToken);
        var overview = new ParkingFacilityOverview(facility, 0, 0, null);
        return Created($"/api/v1/parking-facilities/{facility.Id}",
            ParkingFacilityResourceAssembler.ToResourceFromOverview(overview));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles)]
    [ProducesResponseType(typeof(ParkingFacilityResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFacility(int id, [FromBody] UpdateParkingFacilityResource resource,
        CancellationToken cancellationToken)
    {
        var command = ParkingFacilityResourceAssembler.ToCommandFromResource(id, resource, User.GetUserAccountId());
        await facilityCommandService.Handle(command, cancellationToken);
        var overview = await facilityQueryService.Handle(
                           new GetParkingFacilityByIdQuery(id, User.GetUserAccountId(), true), cancellationToken)
                       ?? throw DomainException.NotFound("facility-not-found", "Parking facility not found.");
        return Ok(ParkingFacilityResourceAssembler.ToResourceFromOverview(overview));
    }
}
