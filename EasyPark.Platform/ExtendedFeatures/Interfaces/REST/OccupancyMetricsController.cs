using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.ValueObjects;
using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Interfaces.REST;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Interfaces.REST;

public record OccupancyMetricInput(int ParkingFacilityId);

[ApiController, Authorize(Roles = ClaimsPrincipalExtensions.OperatorRoles), Route("api/v1/occupancy-metrics"), Tags("Analytics and Reporting")]
public sealed class OccupancyMetricsController(AppDbContext db, ExtendedAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? parkingFacilityId, CancellationToken ct)
    {
        var owned = await access.FacilityIdsAsync(User.GetUserAccountId(), ct);
        return Ok(await db.Set<OccupancyMetric>().AsNoTracking().Where(x => owned.Contains(x.ParkingFacilityId) &&
            (!parkingFacilityId.HasValue || parkingFacilityId == x.ParkingFacilityId)).OrderByDescending(x => x.MeasuredAt).ToListAsync(ct));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<OccupancyMetric>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct), "Metric");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct); return Ok(row);
    }
    [HttpPost]
    public async Task<IActionResult> Create(OccupancyMetricInput input, CancellationToken ct)
    {
        await access.AssertFacilityAsync(input.ParkingFacilityId, User.GetUserAccountId(), ct);
        var row = new OccupancyMetric { ParkingFacilityId = input.ParkingFacilityId };
        await Recalculate(row, ct); db.Set<OccupancyMetric>().Add(row); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OccupancyMetricInput input, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<OccupancyMetric>().FindAsync(new object[] { id }, ct), "Metric");
        await access.AssertFacilityAsync(row.ParkingFacilityId, User.GetUserAccountId(), ct);
        if (input.ParkingFacilityId != row.ParkingFacilityId)
            throw DomainException.BadRequest("facility-change", "Cannot transfer a metric to another facility.");
        await Recalculate(row, ct); await db.SaveChangesAsync(ct); return Ok(row);
    }
    private async Task Recalculate(OccupancyMetric row, CancellationToken ct)
    {
        var spots = await db.Set<ParkingSpot>().AsNoTracking().Where(x => x.FacilityId == row.ParkingFacilityId).ToListAsync(ct);
        row.TotalSpots = spots.Count;
        row.OccupiedSpots = spots.Count(x => x.Status == SpotStatus.Occupied);
        row.ReservedSpots = spots.Count(x => x.Status == SpotStatus.Reserved);
        row.OutOfServiceSpots = spots.Count(x => x.Status == SpotStatus.OutOfService);
        row.OccupancyPercent = spots.Count == 0 ? 0m : Math.Round((decimal)row.OccupiedSpots * 100m / spots.Count, 2);
        if (row.Id == 0) row.MeasuredAt = DateTime.UtcNow;
        row.OccupancyRate = row.OccupancyPercent;
        var from = row.MeasuredAt.AddHours(-1);
        var movements = await db.Set<AccessMovement>().AsNoTracking().Where(x => x.ParkingFacilityId == row.ParkingFacilityId &&
            x.OccurredAt >= from && x.OccurredAt <= row.MeasuredAt).ToListAsync(ct);
        row.EntryCount = movements.Count(x => x.MovementType == MovementType.Entry);
        row.ExitCount = movements.Count(x => x.MovementType == MovementType.Exit);
        var stays = await db.Set<ParkingStay>().AsNoTracking().Where(x => x.ParkingFacilityId == row.ParkingFacilityId &&
            x.EndedAt.HasValue && x.EndedAt.Value >= from && x.EndedAt.Value <= row.MeasuredAt).ToListAsync(ct);
        row.AverageStayMinutes = stays.Count == 0 ? 0m :
            Math.Round((decimal)stays.Average(x => (x.EndedAt!.Value - x.StartedAt).TotalMinutes), 2);
        row.Revenue = stays.Sum(x => x.ChargedAmount ?? 0m);
        row.Currency = "PEN";
    }
}
