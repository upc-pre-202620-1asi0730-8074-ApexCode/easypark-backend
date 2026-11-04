using System.Net.Mime;
using EasyPark.Platform.Profiles.Application.CommandServices;
using EasyPark.Platform.Profiles.Application.QueryServices;
using EasyPark.Platform.Profiles.Domain.Model.Queries;
using EasyPark.Platform.Profiles.Interfaces.REST.Resources;
using EasyPark.Platform.Profiles.Interfaces.REST.Transform;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyPark.Platform.Profiles.Interfaces.REST;

/// <summary>Vehicles and plate numbers.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Profiles and Vehicles")]
public class VehiclesController(
    IVehicleCommandService vehicleCommandService,
    IVehicleQueryService vehicleQueryService,
    IProfileQueryService profileQueryService) : ControllerBase
{
    /// <summary>
    ///     Driver: <c>profileId</c> (own profile) or <c>plateNumber</c>. A plate owned by another driver is not
    ///     disclosed (empty list). Operator: <c>plateNumber</c> lookup.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VehicleResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVehicles([FromQuery] int? profileId, [FromQuery] string? plateNumber,
        CancellationToken cancellationToken)
    {
        var requesterId = User.GetUserAccountId();
        var isOperator = User.IsOperator();
        var ownProfile = await profileQueryService.Handle(new GetProfileByUserAccountIdQuery(requesterId),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(plateNumber))
        {
            var vehicle = await vehicleQueryService.Handle(new GetVehicleByPlateNumberQuery(plateNumber),
                cancellationToken);
            var visible = vehicle is not null &&
                          (isOperator || !vehicle.HasOwner || (ownProfile is not null && vehicle.IsOwnedBy(ownProfile.Id)));
            var found = visible
                ? new List<VehicleResource> { VehicleResourceAssembler.ToResourceFromEntity(vehicle!) }
                : new List<VehicleResource>();
            return Ok(found);
        }

        if (isOperator) return Ok(new List<VehicleResource>());

        if (ownProfile is null) return Ok(new List<VehicleResource>());
        if (profileId.HasValue && profileId.Value != ownProfile.Id)
            throw DomainException.Forbidden("forbidden", "A driver can only list the vehicles of their own profile.");

        var vehicles = await vehicleQueryService.Handle(new GetVehiclesByProfileIdQuery(ownProfile.Id),
            cancellationToken);
        return Ok(vehicles.Select(VehicleResourceAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VehicleResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVehicleById(int id, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleQueryService.Handle(new GetVehicleByIdQuery(id), cancellationToken)
                      ?? throw DomainException.NotFound("vehicle-not-found", "Vehicle not found.");
        if (!User.IsOperator() && vehicle.HasOwner)
        {
            var ownProfile = await profileQueryService.Handle(
                new GetProfileByUserAccountIdQuery(User.GetUserAccountId()), cancellationToken);
            if (ownProfile is null || !vehicle.IsOwnedBy(ownProfile.Id))
                throw DomainException.NotFound("vehicle-not-found", "Vehicle not found.");
        }

        return Ok(VehicleResourceAssembler.ToResourceFromEntity(vehicle));
    }

    /// <summary>Registers a vehicle. Returns 201, 400 (invalid plate/type) or 409 (plate already registered).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleResource resource,
        CancellationToken cancellationToken)
    {
        var command = VehicleResourceAssembler.ToCommandFromResource(resource, User.GetUserAccountId(),
            User.IsOperator());
        var vehicle = await vehicleCommandService.Handle(command, cancellationToken);
        var result = VehicleResourceAssembler.ToResourceFromEntity(vehicle);
        return Created($"/api/v1/vehicles/{result.Id}", result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(VehicleResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleResource resource,
        CancellationToken cancellationToken)
    {
        var command = VehicleResourceAssembler.ToCommandFromResource(id, resource, User.GetUserAccountId(),
            User.IsOperator());
        var vehicle = await vehicleCommandService.Handle(command, cancellationToken);
        return Ok(VehicleResourceAssembler.ToResourceFromEntity(vehicle));
    }
}
