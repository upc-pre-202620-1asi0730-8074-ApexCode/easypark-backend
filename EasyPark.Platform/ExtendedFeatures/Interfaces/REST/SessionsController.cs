using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyPark.Platform.ExtendedFeatures.Interfaces.REST;

public record RefreshSessionInput(string? RefreshToken);

/// <summary>Session renewal, revocation and device/session listing.</summary>
[ApiController, Authorize, Route("api/v1/sessions"), Tags("IAM Sessions")]
public sealed class SessionsController(SessionsService sessions) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Issue(CancellationToken ct) =>
        Ok(await sessions.IssueAsync(User.GetUserAccountId(), ct));

    [HttpPost("refresh"), AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshSessionInput input, CancellationToken ct) =>
        Ok(await sessions.RefreshAsync(input.RefreshToken, ct));

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RefreshSessionInput input, CancellationToken ct)
    {
        await sessions.RevokeAsync(User.GetUserAccountId(), input.RefreshToken, ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await sessions.ListAsync(User.GetUserAccountId(), ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sessions.RevokeByIdAsync(User.GetUserAccountId(), id, ct);
        return NoContent();
    }
}
