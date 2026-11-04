using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyExtendedFeaturesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<AlertRule>(e =>
        {
            e.ToTable("alert_rules"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.AlertType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(12).IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
            e.HasIndex(x => new { x.ParkingFacilityId, x.AlertType }).IsUnique();
            e.HasOne<EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates.ParkingFacility>()
                .WithMany().HasForeignKey(x => x.ParkingFacilityId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Alert>(e =>
        {
            e.ToTable("alerts"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.AlertType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(12).IsRequired();
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
            e.Property(x => x.ResolvedAt).HasColumnType("datetime(6)");
            e.HasIndex(x => new { x.ParkingFacilityId, x.Status });
            e.HasOne<EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates.ParkingFacility>()
                .WithMany().HasForeignKey(x => x.ParkingFacilityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EasyPark.Platform.AccessControl.Domain.Model.Aggregates.ParkingStay>()
                .WithMany().HasForeignKey(x => x.ParkingStayId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EasyPark.Platform.AccessControl.Domain.Model.Aggregates.AccessMovement>()
                .WithMany().HasForeignKey(x => x.AccessMovementId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<NotificationTemplate>(e =>
        {
            e.ToTable("notification_templates"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Type).HasMaxLength(60).IsRequired();
            e.Property(x => x.Locale).HasMaxLength(12).IsRequired();
            e.Property(x => x.TitleTemplate).HasMaxLength(250).IsRequired();
            e.Property(x => x.BodyTemplate).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.Type, x.Locale }).IsUnique();
        });
        builder.Entity<Notification>(e =>
        {
            e.ToTable("notifications"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Channel).HasMaxLength(24).IsRequired();
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
            e.Property(x => x.SentAt).HasColumnType("datetime(6)");
            e.Property(x => x.ReadAt).HasColumnType("datetime(6)");
            e.HasIndex(x => new { x.RecipientId, x.CreatedAt });
            e.HasOne<EasyPark.Platform.Iam.Domain.Model.Aggregates.UserAccount>()
                .WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<NotificationTemplate>()
                .WithMany().HasForeignKey(x => x.NotificationTemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EasyPark.Platform.Reservations.Domain.Model.Aggregates.Reservation>()
                .WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EasyPark.Platform.AccessControl.Domain.Model.Aggregates.ParkingStay>()
                .WithMany().HasForeignKey(x => x.ParkingStayId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Alert>()
                .WithMany().HasForeignKey(x => x.AlertId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<OccupancyMetric>(e =>
        {
            e.ToTable("occupancy_metrics"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.MeasuredAt).HasColumnType("datetime(6)");
            e.Property(x => x.OccupancyPercent).HasColumnType("decimal(6,2)");
            e.Property(x => x.OccupancyRate).HasColumnType("decimal(6,2)");
            e.Property(x => x.AverageStayMinutes).HasColumnType("decimal(12,2)");
            e.Property(x => x.Revenue).HasColumnType("decimal(12,2)");
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.HasIndex(x => new { x.ParkingFacilityId, x.MeasuredAt }).IsUnique();
            e.HasOne<EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates.ParkingFacility>()
                .WithMany().HasForeignKey(x => x.ParkingFacilityId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Report>(e =>
        {
            e.ToTable("reports"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.ReportType).HasMaxLength(30).IsRequired();
            e.Property(x => x.PeriodStart).HasColumnType("datetime(6)");
            e.Property(x => x.PeriodEnd).HasColumnType("datetime(6)");
            e.Property(x => x.GeneratedAt).HasColumnType("datetime(6)");
            e.Property(x => x.Revenue).HasColumnType("decimal(12,2)");
            e.HasIndex(x => new { x.ParkingFacilityId, x.GeneratedAt });
            e.HasOne<EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates.ParkingFacility>()
                .WithMany().HasForeignKey(x => x.ParkingFacilityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EasyPark.Platform.Iam.Domain.Model.Aggregates.UserAccount>()
                .WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
            e.Property(x => x.ExpiresAt).HasColumnType("datetime(6)");
            e.Property(x => x.RevokedAt).HasColumnType("datetime(6)");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserAccountId, x.ExpiresAt });
            e.HasOne<EasyPark.Platform.Iam.Domain.Model.Aggregates.UserAccount>()
                .WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
