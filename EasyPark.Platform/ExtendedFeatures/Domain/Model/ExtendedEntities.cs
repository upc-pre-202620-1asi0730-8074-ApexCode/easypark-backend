using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.ExtendedFeatures.Domain.Model;

/// <summary>Business objects belonging to the three additional contexts and the IAM session store.</summary>
public class AlertRule
{
    public int Id { get; set; }
    public int ParkingFacilityId { get; set; }
    public string AlertType { get; set; } = "";
    public string Severity { get; set; } = "HIGH";
    public int Threshold { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class Alert
{
    public int Id { get; set; }
    public int ParkingFacilityId { get; set; }
    public string AlertType { get; set; } = "";
    public string Severity { get; set; } = "HIGH";
    public string Status { get; set; } = "ACTIVE";
    public int? ContextValue { get; set; }
    public int? ParkingStayId { get; set; }
    public int? AccessMovementId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedBy { get; set; }
    public string? Note { get; set; }
}
public class NotificationTemplate
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Locale { get; set; } = "es";
    public string TitleTemplate { get; set; } = "";
    public string BodyTemplate { get; set; } = "";
}
public class Notification
{
    public int Id { get; set; }
    public int RecipientId { get; set; }
    public int NotificationTemplateId { get; set; }
    public int? ReservationId { get; set; }
    public int? ParkingStayId { get; set; }
    public int? AlertId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Channel { get; set; } = "IN_APP";
    public string Status { get; set; } = "SENT";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
public class OccupancyMetric
{
    public int Id { get; set; }
    public int ParkingFacilityId { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public int TotalSpots { get; set; }
    public int OccupiedSpots { get; set; }
    public int ReservedSpots { get; set; }
    public int OutOfServiceSpots { get; set; }
    public decimal OccupancyPercent { get; set; }
    public decimal OccupancyRate { get; set; }
    public int EntryCount { get; set; }
    public int ExitCount { get; set; }
    public decimal AverageStayMinutes { get; set; }
    public decimal Revenue { get; set; }
    public string Currency { get; set; } = "PEN";
}
public class Report
{
    public int Id { get; set; }
    public int ParkingFacilityId { get; set; }
    public int OperatorId { get; set; }
    public string ReportType { get; set; } = "OCCUPANCY";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalMovements { get; set; }
    public int CompletedStays { get; set; }
    public decimal Revenue { get; set; }
}
public class RefreshToken
{
    public int Id { get; set; }
    public int UserAccountId { get; set; }
    // Only the SHA-256 hash is persisted. Never return this property to callers.
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public static class ExtendedRules
{
    public static string Required(string? value, string name, int length = 200)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > length)
            throw DomainException.BadRequest("invalid-" + name, name + " is required and must not exceed " + length + " characters.");
        return text;
    }

    public static void ValidPeriod(DateTime from, DateTime to)
    {
        if (from == default || to == default || to < from || to - from > TimeSpan.FromDays(366))
            throw DomainException.BadRequest("invalid-period", "Period must be ordered and no longer than 366 days.");
    }
}
