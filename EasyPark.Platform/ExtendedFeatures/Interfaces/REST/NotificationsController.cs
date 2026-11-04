using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Interfaces.REST;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Interfaces.REST;

public record NotificationInput(int RecipientId, int NotificationTemplateId, int? ReservationId, int? ParkingStayId,
    int? AlertId, string Title, string Message, string Channel);
public record NotificationUpdate(string Status);

[ApiController, Authorize, Route("api/v1/notifications"), Tags("Notifications")]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? recipientId, CancellationToken ct)
    {
        var mine = User.GetUserAccountId();
        if (recipientId.HasValue && recipientId.Value != mine)
            throw DomainException.Forbidden("notification-forbidden", "Cannot inspect another account's notifications.");
        return Ok(await db.Set<Notification>().AsNoTracking().Where(x => x.RecipientId == mine)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(ExtendedAccess.Existing(
        await db.Set<Notification>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.RecipientId == User.GetUserAccountId(), ct), "Notification"));
    [HttpPost, Authorize(Roles = "OPERATOR_ADMIN")]
    public async Task<IActionResult> Create(NotificationInput input, CancellationToken ct)
    {
        if (!await db.Set<UserAccount>().AnyAsync(x => x.Id == input.RecipientId, ct))
            throw DomainException.NotFound("recipient-not-found", "Recipient does not exist.");
        if (!await db.Set<NotificationTemplate>().AnyAsync(x => x.Id == input.NotificationTemplateId, ct))
            throw DomainException.NotFound("template-not-found", "Template does not exist.");
        var row = new Notification { RecipientId = input.RecipientId, NotificationTemplateId = input.NotificationTemplateId,
            ReservationId = input.ReservationId, ParkingStayId = input.ParkingStayId, AlertId = input.AlertId,
            Title = ExtendedRules.Required(input.Title, "title", 250), Message = ExtendedRules.Required(input.Message, "message", 2000),
            Channel = ExtendedRules.Required(input.Channel, "channel", 24) };
        db.Set<Notification>().Add(row); await db.SaveChangesAsync(ct);
        return Created($"/api/v1/notifications/{row.Id}", row);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, NotificationUpdate input, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<Notification>().FirstOrDefaultAsync(
            x => x.Id == id && x.RecipientId == User.GetUserAccountId(), ct), "Notification");
        if (input.Status is not ("READ" or "SENT"))
            throw DomainException.BadRequest("invalid-notification-status", "Allowed values: SENT or READ.");
        if (row.Status == "READ" && input.Status == "SENT")
            throw DomainException.Conflict("notification-read", "A read notification cannot return to SENT.");
        row.Status = input.Status;
        if (input.Status == "READ" && row.ReadAt is null) row.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return Ok(row);
    }
}
