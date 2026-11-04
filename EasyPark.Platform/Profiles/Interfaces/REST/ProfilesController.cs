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

/// <summary>Profile of the authenticated driver or operator.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Profiles and Vehicles")]
public class ProfilesController(
    IProfileCommandService profileCommandService,
    IProfileQueryService profileQueryService) : ControllerBase
{
    /// <summary>
    ///     Returns a list with the profile of the authenticated user (empty when it has not been created yet).
    ///     <c>userAccountId</c>, when present, must be the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProfileResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfiles([FromQuery] int? userAccountId, CancellationToken cancellationToken)
    {
        var requesterId = User.GetUserAccountId();
        if (userAccountId.HasValue && userAccountId.Value != requesterId)
            throw DomainException.Forbidden("forbidden", "A user can only read their own profile.");

        var profile = await profileQueryService.Handle(new GetProfileByUserAccountIdQuery(requesterId),
            cancellationToken);
        var resources = profile is null
            ? new List<ProfileResource>()
            : new List<ProfileResource> { ProfileResourceAssembler.ToResourceFromEntity(profile) };
        return Ok(resources);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProfileResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileById(int id, CancellationToken cancellationToken)
    {
        var profile = await profileQueryService.Handle(new GetProfileByIdQuery(id), cancellationToken);
        if (profile is null || !profile.IsOwnedBy(User.GetUserAccountId()))
            throw DomainException.NotFound("profile-not-found", "Profile not found.");
        return Ok(ProfileResourceAssembler.ToResourceFromEntity(profile));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProfileResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileResource resource,
        CancellationToken cancellationToken)
    {
        var command = ProfileResourceAssembler.ToCommandFromResource(resource, User.GetUserAccountId(),
            User.IsOperator());
        var profile = await profileCommandService.Handle(command, cancellationToken);
        var result = ProfileResourceAssembler.ToResourceFromEntity(profile);
        return Created($"/api/v1/profiles/{result.Id}", result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProfileResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileResource resource,
        CancellationToken cancellationToken)
    {
        var command = ProfileResourceAssembler.ToCommandFromResource(id, resource, User.GetUserAccountId(),
            User.IsOperator());
        var profile = await profileCommandService.Handle(command, cancellationToken);
        return Ok(ProfileResourceAssembler.ToResourceFromEntity(profile));
    }
}
