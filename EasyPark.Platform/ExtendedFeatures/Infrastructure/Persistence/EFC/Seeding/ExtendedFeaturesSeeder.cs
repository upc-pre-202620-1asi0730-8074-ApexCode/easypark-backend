using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Infrastructure.Persistence.EFC.Seeding;

/// <summary>Non-sensitive templates required for notifications. Idempotent; safe to seed in all environments.</summary>
public static class ExtendedFeaturesSeeder
{
    public static async Task SeedTemplatesAsync(AppDbContext db, CancellationToken ct = default)
    {
        var defaults = new (string Type, string Locale, string Title, string Body)[]
        {
            ("RESERVATION_CONFIRMED", "es", "Reserva confirmada", "Tu reserva ha sido confirmada."),
            ("RESERVATION_CONFIRMED", "en", "Reservation confirmed", "Your reservation has been confirmed."),
            ("RESERVATION_REMINDER", "es", "Recordatorio de reserva", "Tu reserva comienza pronto."),
            ("RESERVATION_REMINDER", "en", "Reservation reminder", "Your reservation starts soon."),
            ("TIME_REMAINING", "es", "Tiempo restante", "Tu permanencia está por finalizar."),
            ("TIME_REMAINING", "en", "Time remaining", "Your parking stay is ending soon."),
            ("STAY_EXPIRED", "es", "Permanencia vencida", "Has superado el tiempo permitido."),
            ("STAY_EXPIRED", "en", "Parking stay expired", "You exceeded the allowed parking time."),
            ("CHECK_IN_CONFIRMED", "es", "Ingreso registrado", "Se registró el ingreso de tu vehículo."),
            ("CHECK_IN_CONFIRMED", "en", "Check-in confirmed", "Your vehicle check-in was registered."),
            ("CHECK_OUT_CONFIRMED", "es", "Salida registrada", "Se registró la salida de tu vehículo."),
            ("CHECK_OUT_CONFIRMED", "en", "Check-out confirmed", "Your vehicle check-out was registered."),
            ("ALERT_RAISED", "es", "Alerta de estacionamiento", "Se registró una alerta en el estacionamiento."),
            ("ALERT_RAISED", "en", "Parking alert", "An alert was recorded at the parking facility.")
        };
        var existing = await db.Set<NotificationTemplate>().AsNoTracking()
            .Select(x => new { x.Type, x.Locale }).ToListAsync(ct);
        foreach (var (type, locale, title, body) in defaults)
        {
            if (existing.Any(x => x.Type == type && x.Locale == locale)) continue;
            db.Set<NotificationTemplate>().Add(new NotificationTemplate
            {
                Type = type, Locale = locale, TitleTemplate = title, BodyTemplate = body
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
