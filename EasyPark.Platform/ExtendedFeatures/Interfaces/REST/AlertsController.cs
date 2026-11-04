using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Interfaces.REST;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Interfaces.REST;

public record AlertInput(int ParkingFacilityId, string AlertType, string Severity, int? ContextValue, int? ParkingStayId, int? AccessMovementId);
public record AlertUpdate(string Status, string? Note);

[ApiController, Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles), Route("api/v1/alerts"), Tags("Monitoring and Alerts")]
public sealed class AlertsController(AppDbContext db, ExtendedAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? parkingFacilityId, CancellationToken ct)
    {
        var owned = await access.FacilityIdsAsync(User.GetUserAccountId(), ct);
        return Ok(await db.Set<Alert>().AsNoTracking().Where(x => owned.Contains(x.ParkingFacilityId) &&
          (!parkingFacilityId.HasValue || x.ParkingFacilityId == parkingFacilityId)).OrderByDescending(x => x.CreatedAt).ToListAsync(ct));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<Alert>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct), "Alert");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct);
        return Ok(row);
    }
    [HttpPost]
    public async Task<IActionResult> Create(AlertInput input, CancellationToken ct)
    {
        await access.AssertFacilityAsync(input.ParkingFacilityId, User.GetUserAccountId(), ct);
        if (input.ParkingStayId.HasValue && !await db.Set<EasyPark.Platform.AccessControl.Domain.Model.Aggregates.ParkingStay>().AnyAsync(
            x => x.Id == input.ParkingStayId && x.ParkingFacilityId == input.ParkingFacilityId, ct))
            throw DomainException.BadRequest("invalid-stay", "Stay does not belong to the facility.");
        if (input.AccessMovementId.HasValue && !await db.Set<EasyPark.Platform.AccessControl.Domain.Model.Aggregates.AccessMovement>().AnyAsync(
            x => x.Id == input.AccessMovementId && x.ParkingFacilityId == input.ParkingFacilityId, ct))
            throw DomainException.BadRequest("invalid-movement", "Movement does not belong to the facility.");
        var row = new Alert { ParkingFacilityId = input.ParkingFacilityId,
            AlertType = ExtendedRules.Required(input.AlertType, "alert-type", 50),
            Severity = ExtendedRules.Required(input.Severity, "severity", 12),
            ContextValue = input.ContextValue, ParkingStayId = input.ParkingStayId, AccessMovementId = input.AccessMovementId };
        db.Set<Alert>().Add(row); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AlertUpdate input, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<Alert>().FindAsync(new object[] { id }, ct), "Alert");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct);
        if (input.Status != "RESOLVED" && input.Status != "ACTIVE")
            throw DomainException.BadRequest("invalid-status", "Allowed values: ACTIVE or RESOLVED.");
        if (row.Status == "RESOLVED" && input.Status == "ACTIVE")
            throw DomainException.Conflict("alert-resolved", "A resolved alert cannot be reopened.");
        row.Status = input.Status; row.Note = input.Note?.Trim();
        if (row.Note?.Length > 500) throw DomainException.BadRequest("invalid-note", "Note is too long.");
        if (row.Status == "RESOLVED" && row.ResolvedAt is null)
        { row.ResolvedAt = DateTime.UtcNow; row.ResolvedBy = User.GetUserAccountId(); }
        await db.SaveChangesAsync(ct); return Ok(row);
    }
}
