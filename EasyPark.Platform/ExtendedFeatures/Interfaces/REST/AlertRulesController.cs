using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Interfaces.REST;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Interfaces.REST;

public record AlertRuleInput(int ParkingFacilityId, string AlertType, string Severity, int Threshold, bool Enabled);

[ApiController, Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles), Route("api/v1/alert-rules"), Tags("Monitoring and Alerts")]
public sealed class AlertRulesController(AppDbContext db, ExtendedAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? parkingFacilityId, CancellationToken ct)
    {
        var owned = await access.FacilityIdsAsync(User.GetUserAccountId(), ct);
        var rows = await db.Set<AlertRule>().AsNoTracking()
            .Where(x => owned.Contains(x.ParkingFacilityId) && (!parkingFacilityId.HasValue || x.ParkingFacilityId == parkingFacilityId))
            .OrderBy(x => x.Id).ToListAsync(ct);
        return Ok(rows);
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<AlertRule>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct), "Alert rule");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct);
        return Ok(row);
    }
    [HttpPost]
    public async Task<IActionResult> Create(AlertRuleInput input, CancellationToken ct)
    {
        await access.AssertFacilityAsync(input.ParkingFacilityId, User.GetUserAccountId(), ct);
        if (input.Threshold < 0) throw DomainException.BadRequest("invalid-threshold", "Threshold cannot be negative.");
        var row = new AlertRule { ParkingFacilityId = input.ParkingFacilityId,
            AlertType = ExtendedRules.Required(input.AlertType, "alert-type", 50),
            Severity = ExtendedRules.Required(input.Severity, "severity", 12), Threshold = input.Threshold, Enabled = input.Enabled };
        db.Set<AlertRule>().Add(row); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AlertRuleInput input, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<AlertRule>().FindAsync(new object[] { id }, ct), "Alert rule");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct);
        await access.AssertFacilityAsync(input.ParkingFacilityId, User.GetUserAccountId(), ct);
        if (input.ParkingFacilityId != row.ParkingFacilityId) throw DomainException.BadRequest("facility-change", "Cannot transfer an alert rule to another facility.");
        if (input.Threshold < 0) throw DomainException.BadRequest("invalid-threshold", "Threshold cannot be negative.");
        row.AlertType = ExtendedRules.Required(input.AlertType, "alert-type", 50);
        row.Severity = ExtendedRules.Required(input.Severity, "severity", 12);
        row.Threshold = input.Threshold; row.Enabled = input.Enabled; row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return Ok(row);
    }
}
