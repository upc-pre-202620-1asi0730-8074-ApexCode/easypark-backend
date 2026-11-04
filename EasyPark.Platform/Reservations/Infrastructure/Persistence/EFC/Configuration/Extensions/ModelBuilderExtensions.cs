using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Converters;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Reservations.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyReservationsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.Id).ValueGeneratedOnAdd();
            entity.Property(reservation => reservation.Code).HasMaxLength(ReservationCode.MaxLength).IsRequired();
            entity.HasIndex(reservation => reservation.Code).IsUnique().HasDatabaseName("ix_reservations_code");
            entity.Property(reservation => reservation.DriverProfileId).IsRequired();
            entity.Property(reservation => reservation.VehicleId).IsRequired();
            entity.Property(reservation => reservation.ParkingFacilityId).IsRequired();
            entity.Property(reservation => reservation.ParkingSpotId).IsRequired();
            entity.Property(reservation => reservation.StartAt).HasColumnType("datetime(6)").IsRequired();
            entity.Property(reservation => reservation.DurationMinutes).IsRequired();
            entity.Property(reservation => reservation.EstimatedAmount).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(reservation => reservation.Currency).HasMaxLength(3).IsRequired();
            entity.Property(reservation => reservation.Status).HasConversion(new EnumCodeConverter<ReservationStatus>())
                .HasMaxLength(16).IsRequired();
            entity.Property(reservation => reservation.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            entity.Ignore(reservation => reservation.EndAt);
            entity.Ignore(reservation => reservation.IsOpen);
            entity.HasIndex(reservation => reservation.DriverProfileId)
                .HasDatabaseName("ix_reservations_driver_profile_id");
            entity.HasIndex(reservation => new { reservation.VehicleId, reservation.Status })
                .HasDatabaseName("ix_reservations_vehicle_id_status");
            entity.HasIndex(reservation => new { reservation.ParkingSpotId, reservation.Status })
                .HasDatabaseName("ix_reservations_parking_spot_id_status");
            entity.HasIndex(reservation => new { reservation.ParkingFacilityId, reservation.StartAt })
                .HasDatabaseName("ix_reservations_parking_facility_id_start_at");
            entity.HasOne<Profile>().WithMany().HasForeignKey(reservation => reservation.DriverProfileId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_reservations_profiles_driver_profile_id");
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(reservation => reservation.VehicleId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_reservations_vehicles_vehicle_id");
            entity.HasOne<ParkingFacility>().WithMany().HasForeignKey(reservation => reservation.ParkingFacilityId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reservations_parking_facilities_parking_facility_id");
            entity.HasOne<ParkingSpot>().WithMany().HasForeignKey(reservation => reservation.ParkingSpotId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_reservations_parking_spots_parking_spot_id");
        });
    }
}
