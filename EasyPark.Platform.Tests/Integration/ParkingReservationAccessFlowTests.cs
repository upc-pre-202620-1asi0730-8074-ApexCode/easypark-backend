using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyPark.Platform.Tests.Integration;

/// <summary>
///     End-to-end business flow through the HTTP API: operator facility and spots, driver reservation and
///     cancellation, gate entry and exit, and the persisted state of every bounded context.
/// </summary>
public class ParkingReservationAccessFlowTests(EasyParkApiFactory factory) : IClassFixture<EasyParkApiFactory>
{
    private static string NewCarPlate()
    {
        return $"T{Random.Shared.Next(10, 99)}-{Random.Shared.Next(100, 999)}";
    }

    private async Task<(HttpClient client, int facilityId, int carSpotId)> CreateOperatorWithFacilityAsync()
    {
        var client = factory.CreateClient();
        var (_, token) = await client.SignUpAndSignInAsync(ApiClientExtensions.UniqueEmail("operator"), "OPERATOR_ADMIN");
        client.WithToken(token);
        var operatorProfileId = await client.CreateProfileAsync("Operador", companyName: "Parking Test SAC");

        var facilityResponse = await client.PostAsJsonAsync("/api/v1/parking-facilities", new
        {
            operatorProfileId, name = "Sede de prueba", address = "Av. Test 123", latitude = -12.12m,
            longitude = -77.03m, hourlyRate = 6.5m, openTime = "06:00", closeTime = "23:00", status = "ACTIVE"
        });
        Assert.Equal(HttpStatusCode.Created, facilityResponse.StatusCode);
        var facilityId = (await facilityResponse.ReadJsonAsync()).GetProperty("id").GetInt32();

        var spotResponse = await client.PostAsJsonAsync("/api/v1/parking-spots",
            new { facilityId, code = "a-01", level = 1, spotType = "CAR" });
        Assert.Equal(HttpStatusCode.Created, spotResponse.StatusCode);
        var spot = await spotResponse.ReadJsonAsync();
        Assert.Equal("A-01", spot.GetProperty("code").GetString());
        Assert.Equal("AVAILABLE", spot.GetProperty("status").GetString());

        var duplicateCode = await client.PostAsJsonAsync("/api/v1/parking-spots",
            new { facilityId, code = "A-01", level = 1, spotType = "CAR" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateCode.StatusCode);

        var motorcycleSpot = await client.PostAsJsonAsync("/api/v1/parking-spots",
            new { facilityId, code = "M-01", level = 1, spotType = "MOTORCYCLE" });
        Assert.Equal(HttpStatusCode.Created, motorcycleSpot.StatusCode);
        return (client, facilityId, spot.GetProperty("id").GetInt32());
    }

    private async Task<(HttpClient client, int profileId, int vehicleId, string plate)> CreateDriverWithCarAsync()
    {
        var client = factory.CreateClient();
        var (_, token) = await client.SignUpAndSignInAsync(ApiClientExtensions.UniqueEmail("driver"), "DRIVER");
        client.WithToken(token);
        var profileId = await client.CreateProfileAsync("Conductor");
        var plate = NewCarPlate();
        var vehicleResponse = await client.PostAsJsonAsync("/api/v1/vehicles",
            new { profileId, plateNumber = plate, vehicleType = "CAR", isDefault = true });
        Assert.Equal(HttpStatusCode.Created, vehicleResponse.StatusCode);
        var vehicleId = (await vehicleResponse.ReadJsonAsync()).GetProperty("id").GetInt32();
        return (client, profileId, vehicleId, plate);
    }

    private static async Task<string> SpotStatusAsync(HttpClient operatorClient, int spotId)
    {
        var spot = await (await operatorClient.GetAsync($"/api/v1/parking-spots/{spotId}")).ReadJsonAsync();
        return spot.GetProperty("status").GetString()!;
    }

    private static async Task<JsonElement> RegisterAccessAsync(HttpClient operatorClient, int facilityId, string plate,
        string movementType)
    {
        var response = await operatorClient.PostAsJsonAsync("/api/v1/access-movements",
            new { parkingFacilityId = facilityId, plateNumber = plate, movementType, registrationMethod = "MANUAL" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    [Fact]
    public async Task Reservation_entry_exit_flow_updates_every_context()
    {
        var (operatorClient, facilityId, carSpotId) = await CreateOperatorWithFacilityAsync();
        var (driver, profileId, vehicleId, plate) = await CreateDriverWithCarAsync();

        // Driver search shows live availability.
        var facilities = await (await driver.GetAsync("/api/v1/parking-facilities?onlyAvailable=true")).ReadJsonAsync();
        var facility = facilities.EnumerateArray().Single(item => item.GetProperty("id").GetInt32() == facilityId);
        Assert.Equal(2, facility.GetProperty("totalSpots").GetInt32());
        Assert.Equal(2, facility.GetProperty("availableSpots").GetInt32());
        Assert.Equal(6.5m, facility.GetProperty("hourlyRate").GetDecimal());

        // Reservation: the platform assigns the CAR spot and reserves it.
        var startAt = DateTime.UtcNow.AddMinutes(30);
        var create = await driver.PostAsJsonAsync("/api/v1/reservations",
            new { driverProfileId = profileId, vehicleId, parkingFacilityId = facilityId, startAt, durationMinutes = 90 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var reservation = await create.ReadJsonAsync();
        var reservationId = reservation.GetProperty("id").GetInt32();
        Assert.Equal("CONFIRMED", reservation.GetProperty("status").GetString());
        Assert.Equal(carSpotId, reservation.GetProperty("parkingSpotId").GetInt32());
        Assert.Equal(9.75m, reservation.GetProperty("estimatedAmount").GetDecimal());
        Assert.EndsWith("Z", reservation.GetProperty("startAt").GetString());
        Assert.Equal("RESERVED", await SpotStatusAsync(operatorClient, carSpotId));

        // A second driver cannot book the only CAR spot.
        var (secondDriver, _, secondVehicleId, _) = await CreateDriverWithCarAsync();
        var noSpot = await secondDriver.PostAsJsonAsync("/api/v1/reservations",
            new { vehicleId = secondVehicleId, parkingFacilityId = facilityId, startAt, durationMinutes = 60 });
        Assert.Equal(HttpStatusCode.Conflict, noSpot.StatusCode);
        Assert.Equal("no-availability", (await noSpot.ReadJsonAsync()).GetProperty("code").GetString());

        // The operator cannot free a spot held by a reservation.
        var freeReserved = await operatorClient.PutAsJsonAsync($"/api/v1/parking-spots/{carSpotId}",
            new { code = "A-01", level = 1, spotType = "CAR", status = "AVAILABLE" });
        Assert.Equal(HttpStatusCode.Conflict, freeReserved.StatusCode);

        // Entry with the reservation.
        var entry = await RegisterAccessAsync(operatorClient, facilityId, plate.Replace("-", string.Empty), "ENTRY");
        Assert.Equal("COMPLETED", entry.GetProperty("status").GetString());
        Assert.Equal(plate, entry.GetProperty("plateNumber").GetString());
        var stayId = entry.GetProperty("parkingStayId").GetInt32();
        Assert.Equal("OCCUPIED", await SpotStatusAsync(operatorClient, carSpotId));
        var activeReservation = await (await driver.GetAsync($"/api/v1/reservations/{reservationId}")).ReadJsonAsync();
        Assert.Equal("ACTIVE", activeReservation.GetProperty("status").GetString());

        // An ACTIVE reservation cannot be cancelled and a second entry is rejected.
        var cancelActive = await driver.PutAsJsonAsync($"/api/v1/reservations/{reservationId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.Conflict, cancelActive.StatusCode);
        var secondEntry = await RegisterAccessAsync(operatorClient, facilityId, plate, "ENTRY");
        Assert.Equal("REJECTED", secondEntry.GetProperty("status").GetString());
        Assert.Equal("vehicle-already-inside", secondEntry.GetProperty("note").GetString());

        // Exit closes the stay, frees the spot and completes the reservation.
        var exit = await RegisterAccessAsync(operatorClient, facilityId, plate, "EXIT");
        Assert.Equal("COMPLETED", exit.GetProperty("status").GetString());
        Assert.Equal(stayId, exit.GetProperty("parkingStayId").GetInt32());
        Assert.Equal("AVAILABLE", await SpotStatusAsync(operatorClient, carSpotId));
        var stay = await (await operatorClient.GetAsync($"/api/v1/parking-stays/{stayId}")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Number, stay.GetProperty("exitMovementId").ValueKind);
        Assert.True(stay.GetProperty("chargedAmount").GetDecimal() >= 0);

        var exitAgain = await RegisterAccessAsync(operatorClient, facilityId, plate, "EXIT");
        Assert.Equal("no-active-stay", exitAgain.GetProperty("note").GetString());

        // Persisted state.
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await context.Set<Reservation>().AsNoTracking().SingleAsync(item => item.Id == reservationId);
        Assert.Equal(ReservationStatus.Completed, stored.Status);
        var movements = await context.Set<AccessMovement>().AsNoTracking()
            .Where(item => item.ParkingFacilityId == facilityId).ToListAsync();
        Assert.Equal(4, movements.Count);
        var closedStay = await context.Set<ParkingStay>().AsNoTracking().SingleAsync(item => item.Id == stayId);
        Assert.False(closedStay.IsOpen);
    }

    [Fact]
    public async Task Cancelling_a_reservation_frees_the_spot()
    {
        var (operatorClient, facilityId, carSpotId) = await CreateOperatorWithFacilityAsync();
        var (driver, _, vehicleId, _) = await CreateDriverWithCarAsync();

        var create = await driver.PostAsJsonAsync("/api/v1/reservations",
            new { vehicleId, parkingSpotId = carSpotId, startAt = DateTime.UtcNow.AddHours(2), durationMinutes = 60 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var reservationId = (await create.ReadJsonAsync()).GetProperty("id").GetInt32();
        Assert.Equal("RESERVED", await SpotStatusAsync(operatorClient, carSpotId));

        var cancel = await driver.PutAsJsonAsync($"/api/v1/reservations/{reservationId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("CANCELLED", (await cancel.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal("AVAILABLE", await SpotStatusAsync(operatorClient, carSpotId));

        var again = await driver.PutAsJsonAsync($"/api/v1/reservations/{reservationId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        var reactivate = await driver.PutAsJsonAsync($"/api/v1/reservations/{reservationId}", new { status = "CONFIRMED" });
        Assert.Equal(HttpStatusCode.Conflict, reactivate.StatusCode);

        var mine = await (await driver.GetAsync("/api/v1/reservations")).ReadJsonAsync();
        Assert.Equal(1, mine.GetArrayLength());
    }

    [Fact]
    public async Task Reservation_rules_reject_past_dates_foreign_vehicles_and_type_mismatch()
    {
        var (_, facilityId, carSpotId) = await CreateOperatorWithFacilityAsync();
        var (driver, _, vehicleId, _) = await CreateDriverWithCarAsync();
        var (_, _, otherVehicleId, _) = await CreateDriverWithCarAsync();

        var past = await driver.PostAsJsonAsync("/api/v1/reservations",
            new { vehicleId, parkingFacilityId = facilityId, startAt = DateTime.UtcNow.AddHours(-3), durationMinutes = 60 });
        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);

        var foreign = await driver.PostAsJsonAsync("/api/v1/reservations",
            new { vehicleId = otherVehicleId, parkingFacilityId = facilityId, startAt = DateTime.UtcNow.AddHours(1), durationMinutes = 60 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, foreign.StatusCode);

        var spots = await (await driver.GetAsync($"/api/v1/parking-spots?facilityId={facilityId}")).ReadJsonAsync();
        var motorcycleSpotId = spots.EnumerateArray()
            .Single(item => item.GetProperty("spotType").GetString() == "MOTORCYCLE").GetProperty("id").GetInt32();
        var mismatch = await driver.PostAsJsonAsync("/api/v1/reservations",
            new { vehicleId, parkingSpotId = motorcycleSpotId, startAt = DateTime.UtcNow.AddHours(1), durationMinutes = 60 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mismatch.StatusCode);
        Assert.NotEqual(carSpotId, motorcycleSpotId);
    }

    [Fact]
    public async Task Gate_rejects_unknown_plates_and_vehicles_without_reservation()
    {
        var (operatorClient, facilityId, _) = await CreateOperatorWithFacilityAsync();
        var (_, _, _, plate) = await CreateDriverWithCarAsync();

        var unknown = await RegisterAccessAsync(operatorClient, facilityId, NewCarPlate(), "ENTRY");
        Assert.Equal("UNDER_REVIEW", unknown.GetProperty("status").GetString());
        Assert.Equal("vehicle-under-review", unknown.GetProperty("note").GetString());

        var noReservation = await RegisterAccessAsync(operatorClient, facilityId, plate, "ENTRY");
        Assert.Equal("REJECTED", noReservation.GetProperty("status").GetString());
        Assert.Equal("no-active-reservation", noReservation.GetProperty("note").GetString());

        var movements = await (await operatorClient.GetAsync($"/api/v1/access-movements?parkingFacilityId={facilityId}"))
            .ReadJsonAsync();
        Assert.Equal(2, movements.GetArrayLength());
    }

    [Fact]
    public async Task Operators_cannot_manage_other_operators_facilities_and_manual_status_is_validated()
    {
        var (ownerClient, facilityId, carSpotId) = await CreateOperatorWithFacilityAsync();
        var (intruderClient, _, _) = await CreateOperatorWithFacilityAsync();

        var intruderUpdate = await intruderClient.PutAsJsonAsync($"/api/v1/parking-spots/{carSpotId}",
            new { status = "OUT_OF_SERVICE" });
        Assert.Equal(HttpStatusCode.Forbidden, intruderUpdate.StatusCode);
        var intruderAccess = await intruderClient.PostAsJsonAsync("/api/v1/access-movements",
            new { parkingFacilityId = facilityId, plateNumber = "ABC-123", movementType = "ENTRY" });
        Assert.Equal(HttpStatusCode.Forbidden, intruderAccess.StatusCode);

        var outOfService = await ownerClient.PutAsJsonAsync($"/api/v1/parking-spots/{carSpotId}",
            new { status = "OUT_OF_SERVICE" });
        Assert.Equal(HttpStatusCode.OK, outOfService.StatusCode);
        Assert.Equal("OUT_OF_SERVICE", await SpotStatusAsync(ownerClient, carSpotId));

        var reserved = await ownerClient.PutAsJsonAsync($"/api/v1/parking-spots/{carSpotId}", new { status = "RESERVED" });
        Assert.Equal(HttpStatusCode.Conflict, reserved.StatusCode);

        var back = await ownerClient.PutAsJsonAsync($"/api/v1/parking-spots/{carSpotId}", new { status = "AVAILABLE" });
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Equal("AVAILABLE", await SpotStatusAsync(ownerClient, carSpotId));

        var ownFacilities = await (await ownerClient.GetAsync("/api/v1/parking-facilities")).ReadJsonAsync();
        Assert.Equal(1, ownFacilities.GetArrayLength());
    }
}
