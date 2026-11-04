using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Interfaces.REST;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Interfaces.REST;

public record ReportInput(int ParkingFacilityId, string ReportType, DateTime PeriodStart, DateTime PeriodEnd);

[ApiController, Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles), Route("api/v1/reports"), Tags("Analytics and Reporting")]
public sealed class ReportsController(AppDbContext db, ExtendedAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? parkingFacilityId, CancellationToken ct)
    {
        var owned = await access.FacilityIdsAsync(User.GetUserAccountId(), ct);
        return Ok(await db.Set<Report>().AsNoTracking().Where(x => owned.Contains(x.ParkingFacilityId) &&
            (!parkingFacilityId.HasValue || parkingFacilityId == x.ParkingFacilityId)).OrderByDescending(x => x.GeneratedAt).ToListAsync(ct));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<Report>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct), "Report");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct); return Ok(row);
    }
    [HttpPost]
    public async Task<IActionResult> Create(ReportInput input, CancellationToken ct)
    {
        await access.AssertFacilityAsync(input.ParkingFacilityId, User.GetUserAccountId(), ct);
        ExtendedRules.ValidPeriod(input.PeriodStart, input.PeriodEnd);
        var row = new Report { ParkingFacilityId = input.ParkingFacilityId, OperatorId = User.GetUserAccountId(),
            ReportType = ExtendedRules.Required(input.ReportType, "report-type", 30),
            PeriodStart = input.PeriodStart, PeriodEnd = input.PeriodEnd };
        await Recalculate(row, ct); db.Set<Report>().Add(row); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ReportInput input, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<Report>().FindAsync(new object[] { id }, ct), "Report");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct);
        if (input.ParkingFacilityId != row.ParkingFacilityId)
            throw DomainException.BadRequest("facility-change", "Cannot transfer reports between facilities.");
        ExtendedRules.ValidPeriod(input.PeriodStart, input.PeriodEnd);
        row.ReportType = ExtendedRules.Required(input.ReportType, "report-type", 30);
        row.PeriodStart = input.PeriodStart; row.PeriodEnd = input.PeriodEnd;
        await Recalculate(row, ct); await db.SaveChangesAsync(ct); return Ok(row);
    }
    private async Task Recalculate(Report row, CancellationToken ct)
    {
        row.TotalMovements = await db.Set<AccessMovement>().CountAsync(x => x.ParkingFacilityId == row.ParkingFacilityId &&
            x.OccurredAt >= row.PeriodStart && x.OccurredAt <= row.PeriodEnd, ct);
        var stays = await db.Set<ParkingStay>().AsNoTracking().Where(x => x.ParkingFacilityId == row.ParkingFacilityId &&
            x.EndedAt.HasValue && x.EndedAt.Value >= row.PeriodStart && x.EndedAt.Value <= row.PeriodEnd).ToListAsync(ct);
        row.CompletedStays = stays.Count;
        row.Revenue = stays.Sum(x => x.ChargedAmount ?? 0m);
        row.GeneratedAt = DateTime.UtcNow;
    }
}
