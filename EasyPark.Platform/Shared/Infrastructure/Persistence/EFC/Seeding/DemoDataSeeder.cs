using EasyPark.Platform.Iam.Application.Internal.OutboundServices;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Seeding;

/// <summary>
///     Demonstration data for LOCAL environments only (enabled with <c>SeedDemoData=true</c>).
///     It runs only when the database has no user accounts. Never enable it in production.
/// </summary>
public static class DemoDataSeeder
{
    public const string DriverEmail = "conductor.demo@easypark.local";
    public const string OperatorEmail = "operador.demo@easypark.local";
    public const string DemoPassword = "Demo12345!";

    public static async Task SeedAsync(AppDbContext context, IHashingService hashingService,
        CancellationToken cancellationToken = default)
    {
        if (await context.Set<UserAccount>().AnyAsync(cancellationToken)) return;

        var driver = new UserAccount(EmailAddress.Create(DriverEmail), hashingService.HashPassword(DemoPassword),
            UserRole.Driver);
        var operatorAccount = new UserAccount(EmailAddress.Create(OperatorEmail),
            hashingService.HashPassword(DemoPassword), UserRole.OperatorAdmin);
        context.AddRange(driver, operatorAccount);
        await context.SaveChangesAsync(cancellationToken);

        var driverProfile = new Profile(driver.Id, "Conductor", "Demo", "+51 900 000 001", null, null, null);
        var operatorProfile = new Profile(operatorAccount.Id, "Operador", "Demo", "+51 900 000 002", null,
            "EasyPark Demo S.A.C.", "Administrador de sede");
        context.AddRange(driverProfile, operatorProfile);
        await context.SaveChangesAsync(cancellationToken);

        var car = new Vehicle(PlateNumber.Create("ABC-123"), driverProfile.Id, VehicleType.Car, "Gris");
        car.MarkAsDefault();
        context.Add(car);

        var facility = new ParkingFacility(operatorProfile.Id, "Estacionamiento Demo Miraflores",
            "Av. Larco 345, Miraflores, Lima", -12.1215m, -77.0297m, 6.50m, "06:00", "23:00", FacilityStatus.Active);
        context.Add(facility);
        await context.SaveChangesAsync(cancellationToken);

        context.AddRange(
            new ParkingSpot(facility.Id, "A-01", 1, SpotType.Car),
            new ParkingSpot(facility.Id, "A-02", 1, SpotType.Car),
            new ParkingSpot(facility.Id, "A-03", 1, SpotType.Suv),
            new ParkingSpot(facility.Id, "M-01", 1, SpotType.Motorcycle));
        await context.SaveChangesAsync(cancellationToken);
    }
}
