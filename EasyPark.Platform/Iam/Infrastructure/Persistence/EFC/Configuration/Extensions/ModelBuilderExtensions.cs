using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Converters;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Iam.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Id).ValueGeneratedOnAdd();
            entity.Property(account => account.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
            entity.HasIndex(account => account.Email).IsUnique().HasDatabaseName("ix_user_accounts_email");
            entity.Property(account => account.PasswordHash).HasMaxLength(100).IsRequired();
            entity.Property(account => account.Role).HasConversion(new EnumCodeConverter<UserRole>())
                .HasMaxLength(32).IsRequired();
            entity.Property(account => account.Status).HasConversion(new EnumCodeConverter<AccountStatus>())
                .HasMaxLength(16).IsRequired();
            entity.Property(account => account.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            entity.Property(account => account.LastLoginAt).HasColumnType("datetime(6)");
            entity.Ignore(account => account.IsActive);
            entity.Ignore(account => account.IsOperator);
        });
    }
}
