using System.Net;
using System.Net.Http.Json;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyPark.Platform.Tests.Integration;

public class IamAndProfilesApiTests(EasyParkApiFactory factory) : IClassFixture<EasyParkApiFactory>
{
    [Fact]
    public async Task Sign_up_returns_201_and_stores_a_bcrypt_hash()
    {
        var client = factory.CreateClient();
        var email = ApiClientExtensions.UniqueEmail("driver");

        var response = await client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new { email = email.ToUpperInvariant(), password = "Password123!", role = "DRIVER" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("DRIVER", body.GetProperty("role").GetString());

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await context.Set<UserAccount>().SingleAsync(account => account.Email == email);
        Assert.NotEqual("Password123!", stored.PasswordHash);
        Assert.StartsWith("$2", stored.PasswordHash);
    }

    [Fact]
    public async Task Sign_up_rejects_duplicates_invalid_roles_and_short_passwords()
    {
        var client = factory.CreateClient();
        var email = ApiClientExtensions.UniqueEmail("dup");
        await client.SignUpAndSignInAsync(email, "DRIVER");

        var duplicate = await client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new { email, password = "Password123!", role = "DRIVER" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("email-taken", (await duplicate.ReadJsonAsync()).GetProperty("code").GetString());

        var staff = await client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new { email = ApiClientExtensions.UniqueEmail("staff"), password = "Password123!", role = "OPERATOR_STAFF" });
        Assert.Equal(HttpStatusCode.BadRequest, staff.StatusCode);

        var shortPassword = await client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new { email = ApiClientExtensions.UniqueEmail("short"), password = "123", role = "DRIVER" });
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);
    }

    [Fact]
    public async Task Sign_in_returns_jwt_and_rejects_wrong_password()
    {
        var client = factory.CreateClient();
        var email = ApiClientExtensions.UniqueEmail("signin");
        var (userId, token) = await client.SignUpAndSignInAsync(email, "OPERATOR_ADMIN");
        Assert.True(userId > 0);
        Assert.Equal(3, token.Split('.').Length);

        var wrong = await client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email, password = "WrongPassword!" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_valid_token_and_the_right_role()
    {
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/profiles")).StatusCode);

        var forged = factory.CreateClient().WithToken("not.a.valid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await forged.GetAsync("/api/v1/profiles")).StatusCode);

        var driver = factory.CreateClient();
        var (_, token) = await driver.SignUpAndSignInAsync(ApiClientExtensions.UniqueEmail("role"), "DRIVER");
        driver.WithToken(token);
        var createFacility = await driver.PostAsJsonAsync("/api/v1/parking-facilities",
            new { name = "X", address = "Y", hourlyRate = 5 });
        Assert.Equal(HttpStatusCode.Forbidden, createFacility.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/access-movements")).StatusCode);
    }

    [Fact]
    public async Task Change_password_validates_the_current_password()
    {
        var client = factory.CreateClient();
        var email = ApiClientExtensions.UniqueEmail("password");
        var (_, token) = await client.SignUpAndSignInAsync(email, "DRIVER");
        client.WithToken(token);

        var wrong = await client.PutAsJsonAsync("/api/v1/authentication/password",
            new { currentPassword = "nope-nope", newPassword = "NewPassword123!" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);

        var ok = await client.PutAsJsonAsync("/api/v1/authentication/password",
            new { currentPassword = "Password123!", newPassword = "NewPassword123!" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var signIn = await factory.CreateClient().PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email, password = "NewPassword123!" });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
    }

    [Fact]
    public async Task Profile_and_vehicles_follow_ownership_rules()
    {
        var driver = factory.CreateClient();
        var (driverId, token) = await driver.SignUpAndSignInAsync(ApiClientExtensions.UniqueEmail("owner"), "DRIVER");
        driver.WithToken(token);

        var empty = await (await driver.GetAsync($"/api/v1/profiles?userAccountId={driverId}")).ReadJsonAsync();
        Assert.Equal(0, empty.GetArrayLength());
        var profileId = await driver.CreateProfileAsync("Camila", companyName: "Ignored for drivers");

        var profiles = await (await driver.GetAsync($"/api/v1/profiles?userAccountId={driverId}")).ReadJsonAsync();
        Assert.Equal(1, profiles.GetArrayLength());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, profiles[0].GetProperty("companyName").ValueKind);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await driver.GetAsync($"/api/v1/profiles?userAccountId={driverId + 1000}")).StatusCode);

        var duplicateProfile = await driver.PostAsJsonAsync("/api/v1/profiles",
            new { firstName = "A", lastName = "B", phone = "123456" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateProfile.StatusCode);

        var plate = $"Q{Random.Shared.Next(10, 99)}{Random.Shared.Next(100, 999)}";
        var created = await driver.PostAsJsonAsync("/api/v1/vehicles",
            new { profileId, plateNumber = plate.ToLowerInvariant(), vehicleType = "CAR", color = "Rojo", isDefault = false });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var vehicle = await created.ReadJsonAsync();
        Assert.Equal($"{plate[..3]}-{plate[3..]}", vehicle.GetProperty("plateNumber").GetString());
        Assert.True(vehicle.GetProperty("isDefault").GetBoolean());

        var other = factory.CreateClient();
        var (_, otherToken) = await other.SignUpAndSignInAsync(ApiClientExtensions.UniqueEmail("other"), "DRIVER");
        other.WithToken(otherToken);
        await other.CreateProfileAsync("Diego");
        var lookup = await (await other.GetAsync($"/api/v1/vehicles?plateNumber={plate}")).ReadJsonAsync();
        Assert.Equal(0, lookup.GetArrayLength());
        var stolen = await other.PostAsJsonAsync("/api/v1/vehicles",
            new { plateNumber = plate, vehicleType = "CAR", isDefault = true });
        Assert.Equal(HttpStatusCode.Conflict, stolen.StatusCode);
        Assert.Equal("plate-taken", (await stolen.ReadJsonAsync()).GetProperty("code").GetString());
    }
}
