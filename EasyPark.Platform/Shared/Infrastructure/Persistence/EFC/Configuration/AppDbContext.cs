using EasyPark.Platform.AccessControl.Infrastructure.Persistence.EFC.Configuration.Extensions;
using EasyPark.Platform.ExtendedFeatures.Infrastructure.Persistence.EFC.Configuration.Extensions;
using EasyPark.Platform.Iam.Infrastructure.Persistence.EFC.Configuration.Extensions;
using EasyPark.Platform.ParkingManagement.Infrastructure.Persistence.EFC.Configuration.Extensions;
using EasyPark.Platform.Profiles.Infrastructure.Persistence.EFC.Configuration.Extensions;
using EasyPark.Platform.Reservations.Infrastructure.Persistence.EFC.Configuration.Extensions;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Single EF Core context of the modular monolith. Each bounded context contributes its own mapping
///     through an <c>Apply...Configuration</c> extension method (FruitLogix pattern).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyIamConfiguration();
        builder.ApplyProfilesConfiguration();
        builder.ApplyParkingManagementConfiguration();
        builder.ApplyReservationsConfiguration();
        builder.ApplyAccessControlConfiguration();
        builder.ApplyExtendedFeaturesConfiguration();

        builder.UseSnakeCaseColumnNames();
        builder.UseUtcDateTimes();
    }
}
