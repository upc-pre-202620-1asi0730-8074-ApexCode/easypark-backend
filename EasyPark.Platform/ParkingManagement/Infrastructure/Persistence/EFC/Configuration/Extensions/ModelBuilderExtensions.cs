using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Converters;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ParkingManagement.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyParkingManagementConfiguration(this ModelBuilder builder)
    {
        builder.Entity<ParkingFacility>(entity =>
        {
            entity.ToTable("parking_facilities");
            entity.HasKey(facility => facility.Id);
            entity.Property(facility => facility.Id).ValueGeneratedOnAdd();
            entity.Property(facility => facility.OperatorProfileId).IsRequired();
            entity.HasIndex(facility => facility.OperatorProfileId)
                .HasDatabaseName("ix_parking_facilities_operator_profile_id");
            entity.Property(facility => facility.Name).HasMaxLength(180).IsRequired();
            entity.Property(facility => facility.Address).HasMaxLength(300).IsRequired();
            entity.Property(facility => facility.Latitude).HasColumnType("decimal(10,7)");
            entity.Property(facility => facility.Longitude).HasColumnType("decimal(10,7)");
            entity.Property(facility => facility.HourlyRate).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(facility => facility.OpenTime).HasMaxLength(5).IsRequired();
            entity.Property(facility => facility.CloseTime).HasMaxLength(5).IsRequired();
            entity.Property(facility => facility.Status).HasConversion(new EnumCodeConverter<FacilityStatus>())
                .HasMaxLength(16).IsRequired();
            entity.Property(facility => facility.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            entity.Ignore(facility => facility.IsActive);
            entity.HasOne<Profile>().WithMany().HasForeignKey(facility => facility.OperatorProfileId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_parking_facilities_profiles_operator_profile_id");
        });

        builder.Entity<ParkingSpot>(entity =>
        {
            entity.ToTable("parking_spots");
            entity.HasKey(spot => spot.Id);
            entity.Property(spot => spot.Id).ValueGeneratedOnAdd();
            entity.Property(spot => spot.FacilityId).IsRequired();
            entity.HasIndex(spot => new { spot.FacilityId, spot.Code }).IsUnique()
                .HasDatabaseName("ix_parking_spots_facility_id_code");
            entity.Property(spot => spot.Code).HasMaxLength(30).IsRequired();
            entity.Property(spot => spot.Level).IsRequired();
            entity.Property(spot => spot.SpotType).HasConversion(new EnumCodeConverter<SpotType>())
                .HasMaxLength(16).IsRequired();
            entity.Property(spot => spot.Status).HasConversion(new EnumCodeConverter<SpotStatus>())
                .HasMaxLength(16).IsRequired();
            entity.Property(spot => spot.Version).IsRequired().IsConcurrencyToken();
            entity.Property(spot => spot.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            entity.Ignore(spot => spot.IsAvailable);
            entity.HasOne<ParkingFacility>().WithMany().HasForeignKey(spot => spot.FacilityId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_parking_spots_parking_facilities_facility_id");
        });
    }
}
