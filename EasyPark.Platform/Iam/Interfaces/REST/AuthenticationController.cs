using System.Net.Mime;
using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.Iam.Application.CommandServices;
using EasyPark.Platform.Iam.Interfaces.REST.Resources;
using EasyPark.Platform.Iam.Interfaces.REST.Transform;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyPark.Platform.Iam.Interfaces.REST;

/// <summary>
///     Identity and Access Management endpoints: sign-up, sign-in and password change.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("IAM")]
public class AuthenticationController(IUserAccountCommandService userAccountCommandService, SessionsService sessionsService) : ControllerBase
{
    /// <summary>Registers a DRIVER or OPERATOR_ADMIN account. Returns 201, 400 (invalid data) or 409 (e-mail taken).</summary>
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserAccountResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] SignUpResource resource, CancellationToken cancellationToken)
    {
        var command = SignUpCommandFromResourceAssembler.ToCommandFromResource(resource);
        var userAccount = await userAccountCommandService.Handle(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            UserAccountResourceFromEntityAssembler.ToResourceFromEntity(userAccount));
    }

    /// <summary>Validates the credentials and returns a JWT. Returns 200, 401 (invalid credentials) or 403 (inactive).</summary>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticatedUserAccountResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SignIn([FromBody] SignInResource resource, CancellationToken cancellationToken)
    {
        var command = SignInCommandFromResourceAssembler.ToCommandFromResource(resource);
        var (userAccount, token) = await userAccountCommandService.Handle(command, cancellationToken);
        var refresh = await sessionsService.IssueAsync(userAccount.Id, cancellationToken);
        return Ok(UserAccountResourceFromEntityAssembler.ToAuthenticatedResource(userAccount, token) with
        {
            RefreshToken = refresh.RefreshToken
        });
    }

    /// <summary>Changes the password of the authenticated account. Returns 200, 400, 401 or 422 (wrong current password).</summary>
    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordResource resource,
        CancellationToken cancellationToken)
    {
        var command = ChangePasswordCommandFromResourceAssembler.ToCommandFromResource(User.GetUserAccountId(), resource);
        await userAccountCommandService.Handle(command, cancellationToken);
        await sessionsService.RevokeAllAsync(User.GetUserAccountId(), cancellationToken);
        return Ok(new MessageResource("Password updated."));
    }
}
