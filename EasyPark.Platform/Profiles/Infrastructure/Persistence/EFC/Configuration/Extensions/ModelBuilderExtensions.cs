using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Converters;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Profiles.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyProfilesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Id).ValueGeneratedOnAdd();
            entity.Property(profile => profile.UserAccountId).IsRequired();
            entity.HasIndex(profile => profile.UserAccountId).IsUnique()
                .HasDatabaseName("ix_profiles_user_account_id");
            entity.Property(profile => profile.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(profile => profile.LastName).HasMaxLength(120).IsRequired();
            entity.Property(profile => profile.Phone).HasMaxLength(40).IsRequired();
            entity.Property(profile => profile.PhotoUrl).HasMaxLength(1000);
            entity.Property(profile => profile.CompanyName).HasMaxLength(180);
            entity.Property(profile => profile.JobTitle).HasMaxLength(120);
            entity.Property(profile => profile.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(profile => profile.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_profiles_user_accounts_user_account_id");
        });

        builder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles");
            entity.HasKey(vehicle => vehicle.Id);
            entity.Property(vehicle => vehicle.Id).ValueGeneratedOnAdd();
            entity.Property(vehicle => vehicle.ProfileId).IsRequired(false);
            entity.HasIndex(vehicle => vehicle.ProfileId).HasDatabaseName("ix_vehicles_profile_id");
            entity.Property(vehicle => vehicle.PlateNumber).HasMaxLength(PlateNumber.MaxLength).IsRequired();
            entity.HasIndex(vehicle => vehicle.PlateNumber).IsUnique().HasDatabaseName("ix_vehicles_plate_number");
            entity.Property(vehicle => vehicle.VehicleType).HasConversion(new EnumCodeConverter<VehicleType>())
                .HasMaxLength(16).IsRequired();
            entity.Property(vehicle => vehicle.Color).HasMaxLength(60);
            entity.Property(vehicle => vehicle.IsDefault).HasColumnType("tinyint(1)").IsRequired();
            entity.Property(vehicle => vehicle.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            entity.Ignore(vehicle => vehicle.HasOwner);
            entity.HasOne<Profile>().WithMany().HasForeignKey(vehicle => vehicle.ProfileId).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_vehicles_profiles_profile_id");
        });
    }
}
