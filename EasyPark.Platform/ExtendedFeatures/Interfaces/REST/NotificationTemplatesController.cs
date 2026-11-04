using EasyPark.Platform.ExtendedFeatures.Application;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Interfaces.REST;

public record NotificationTemplateInput(string Type, string Locale, string TitleTemplate, string BodyTemplate);

[ApiController, Authorize, Route("api/v1/notification-templates"), Tags("Notifications")]
public sealed class NotificationTemplatesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await db.Set<NotificationTemplate>().AsNoTracking()
        .OrderBy(x => x.Id).ToListAsync(ct));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(ExtendedAccess.Existing(
        await db.Set<NotificationTemplate>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct), "Template"));
    [HttpPost, Authorize(Roles = "OPERATOR_ADMIN")]
    public async Task<IActionResult> Create(NotificationTemplateInput input, CancellationToken ct)
    {
        var row = new NotificationTemplate(); Fill(row, input);
        db.Set<NotificationTemplate>().Add(row); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row);
    }
    [HttpPut("{id:int}"), Authorize(Roles = "OPERATOR_ADMIN")]
    public async Task<IActionResult> Update(int id, NotificationTemplateInput input, CancellationToken ct)
    {
        var row = ExtendedAccess.Existing(await db.Set<NotificationTemplate>().FindAsync(new object[] { id }, ct), "Template");
        Fill(row, input); await db.SaveChangesAsync(ct); return Ok(row);
    }
    private static void Fill(NotificationTemplate row, NotificationTemplateInput input)
    {
        row.Type = ExtendedRules.Required(input.Type, "notification-type", 60);
        row.Locale = ExtendedRules.Required(input.Locale, "locale", 12);
        row.TitleTemplate = ExtendedRules.Required(input.TitleTemplate, "title-template", 250);
        row.BodyTemplate = ExtendedRules.Required(input.BodyTemplate, "body-template", 2000);
    }
}
