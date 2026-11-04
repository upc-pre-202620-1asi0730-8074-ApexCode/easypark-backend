using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.ValueObjects;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Converters;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.AccessControl.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyAccessControlConfiguration(this ModelBuilder builder)
    {
        builder.Entity<AccessMovement>(entity =>
        {
            entity.ToTable("access_movements");
            entity.HasKey(movement => movement.Id);
            entity.Property(movement => movement.Id).ValueGeneratedOnAdd();
            entity.Property(movement => movement.ParkingFacilityId).IsRequired();
            entity.Property(movement => movement.VehicleId).IsRequired();
            entity.Property(movement => movement.PlateNumber).HasMaxLength(12).IsRequired();
            entity.Property(movement => movement.OperatorId).IsRequired();
            entity.Property(movement => movement.MovementType).HasConversion(new EnumCodeConverter<MovementType>())
                .HasMaxLength(8).IsRequired();
            entity.Property(movement => movement.RegistrationMethod)
                .HasConversion(new EnumCodeConverter<RegistrationMethod>()).HasMaxLength(16).IsRequired();
            entity.Property(movement => movement.Status).HasConversion(new EnumCodeConverter<MovementStatus>())
                .HasMaxLength(16).IsRequired();
            entity.Property(movement => movement.OccurredAt).HasColumnType("datetime(6)").IsRequired();
            entity.Property(movement => movement.Note).HasMaxLength(200);
            entity.HasIndex(movement => new { movement.ParkingFacilityId, movement.OccurredAt })
                .HasDatabaseName("ix_access_movements_parking_facility_id_occurred_at");
            entity.HasIndex(movement => movement.VehicleId).HasDatabaseName("ix_access_movements_vehicle_id");
            entity.HasIndex(movement => movement.OperatorId).HasDatabaseName("ix_access_movements_operator_id");
            entity.HasOne<ParkingFacility>().WithMany().HasForeignKey(movement => movement.ParkingFacilityId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_movements_parking_facilities_parking_facility_id");
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(movement => movement.VehicleId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_access_movements_vehicles_vehicle_id");
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(movement => movement.OperatorId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_access_movements_user_accounts_operator_id");
        });

        builder.Entity<ParkingStay>(entity =>
        {
            entity.ToTable("parking_stays");
            entity.HasKey(stay => stay.Id);
            entity.Property(stay => stay.Id).ValueGeneratedOnAdd();
            entity.Property(stay => stay.ParkingFacilityId).IsRequired();
            entity.Property(stay => stay.ParkingSpotId).IsRequired();
            entity.Property(stay => stay.VehicleId).IsRequired();
            entity.Property(stay => stay.ReservationId).IsRequired(false);
            entity.Property(stay => stay.EntryMovementId).IsRequired();
            entity.Property(stay => stay.ExitMovementId).IsRequired(false);
            entity.Property(stay => stay.ChargedAmount).HasColumnType("decimal(10,2)");
            entity.Property(stay => stay.Currency).HasMaxLength(3).IsRequired();
            entity.Property(stay => stay.StartedAt).HasColumnType("datetime(6)").IsRequired();
            entity.Property(stay => stay.EndedAt).HasColumnType("datetime(6)");
            entity.Ignore(stay => stay.IsOpen);
            entity.HasIndex(stay => stay.EntryMovementId).IsUnique()
                .HasDatabaseName("ix_parking_stays_entry_movement_id");
            entity.HasIndex(stay => stay.ExitMovementId).IsUnique()
                .HasDatabaseName("ix_parking_stays_exit_movement_id");
            entity.HasIndex(stay => new { stay.VehicleId, stay.ExitMovementId })
                .HasDatabaseName("ix_parking_stays_vehicle_id_exit_movement_id");
            entity.HasIndex(stay => new { stay.ParkingSpotId, stay.ExitMovementId })
                .HasDatabaseName("ix_parking_stays_parking_spot_id_exit_movement_id");
            entity.HasIndex(stay => stay.ParkingFacilityId).HasDatabaseName("ix_parking_stays_parking_facility_id");
            entity.HasIndex(stay => stay.ReservationId).HasDatabaseName("ix_parking_stays_reservation_id");
            entity.HasOne<ParkingFacility>().WithMany().HasForeignKey(stay => stay.ParkingFacilityId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_parking_stays_parking_facilities_parking_facility_id");
            entity.HasOne<ParkingSpot>().WithMany().HasForeignKey(stay => stay.ParkingSpotId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_parking_stays_parking_spots_parking_spot_id");
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(stay => stay.VehicleId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_parking_stays_vehicles_vehicle_id");
            entity.HasOne<Reservation>().WithMany().HasForeignKey(stay => stay.ReservationId).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_parking_stays_reservations_reservation_id");
            entity.HasOne<AccessMovement>().WithMany().HasForeignKey(stay => stay.EntryMovementId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_parking_stays_access_movements_entry_movement_id");
            entity.HasOne<AccessMovement>().WithMany().HasForeignKey(stay => stay.ExitMovementId).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_parking_stays_access_movements_exit_movement_id");
        });
    }
}
