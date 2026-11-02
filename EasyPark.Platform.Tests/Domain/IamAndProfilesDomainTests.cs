using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Tests.Domain;

public class IamAndProfilesDomainTests
{
    [Theory]
    [InlineData("  Camila.Soto@Example.COM ", "camila.soto@example.com")]
    [InlineData("a@b.pe", "a@b.pe")]
    public void Email_is_normalized(string input, string expected)
    {
        Assert.Equal(expected, EmailAddress.Create(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("a@b")]
    [InlineData("a b@c.pe")]
    public void Invalid_emails_are_rejected(string input)
    {
        Assert.False(EmailAddress.TryCreate(input, out _));
        Assert.Throws<DomainException>(() => EmailAddress.Create(input));
    }

    [Fact]
    public void Only_driver_and_operator_admin_can_self_register()
    {
        Assert.True(UserAccount.CanSelfRegisterAs(UserRole.Driver));
        Assert.True(UserAccount.CanSelfRegisterAs(UserRole.OperatorAdmin));
        Assert.False(UserAccount.CanSelfRegisterAs(UserRole.OperatorStaff));
    }

    [Fact]
    public void Password_policy_requires_eight_characters()
    {
        Assert.Throws<DomainException>(() => UserAccount.EnsurePasswordPolicy("short", "invalid-password"));
        UserAccount.EnsurePasswordPolicy("long-enough", "invalid-password");
    }

    [Fact]
    public void Bcrypt_hash_is_not_plain_text_and_verifies()
    {
        var hashing = new HashingService();
        var hash = hashing.HashPassword("Password123!");
        Assert.NotEqual("Password123!", hash);
        Assert.StartsWith("$2", hash);
        Assert.True(hashing.VerifyPassword("Password123!", hash));
        Assert.False(hashing.VerifyPassword("wrong-password", hash));
        Assert.False(hashing.VerifyPassword("Password123!", "not-a-bcrypt-hash"));
    }

    [Fact]
    public void New_accounts_are_active()
    {
        var account = new UserAccount(EmailAddress.Create("x@y.pe"), "hash", UserRole.Driver);
        Assert.True(account.IsActive);
        Assert.False(account.IsOperator);
        account.Deactivate();
        Assert.False(account.IsActive);
    }

    [Theory]
    [InlineData("abc123", "ABC-123", false)]
    [InlineData("B7K-482", "B7K-482", false)]
    [InlineData(" 1234 ab ", "1234-AB", true)]
    public void Plate_numbers_are_normalized(string input, string expected, bool isMotorcycle)
    {
        var plate = PlateNumber.Create(input);
        Assert.Equal(expected, plate.Value);
        Assert.Equal(isMotorcycle, plate.IsMotorcyclePlate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB-12")]
    [InlineData("ABCD-123")]
    public void Invalid_plate_numbers_are_rejected(string input)
    {
        Assert.False(PlateNumber.TryCreate(input, out _));
    }

    [Fact]
    public void Unowned_vehicle_infers_its_type_and_can_be_claimed_once()
    {
        var vehicle = Vehicle.RegisterUnowned(PlateNumber.Create("1234AB"));
        Assert.False(vehicle.HasOwner);
        Assert.Equal(VehicleType.Motorcycle, vehicle.VehicleType);

        vehicle.AssignOwner(7);
        Assert.True(vehicle.IsOwnedBy(7));
        var exception = Assert.Throws<DomainException>(() => vehicle.AssignOwner(8));
        Assert.Equal("plate-taken", exception.Code);
    }

    [Fact]
    public void Releasing_a_vehicle_removes_owner_and_default_flag()
    {
        var vehicle = new Vehicle(PlateNumber.Create("ABC-123"), 3, VehicleType.Car, "Gris");
        vehicle.MarkAsDefault();
        vehicle.ReleaseOwner();
        Assert.False(vehicle.HasOwner);
        Assert.False(vehicle.IsDefault);
        Assert.Throws<DomainException>(() => vehicle.MarkAsDefault());
    }

    [Fact]
    public void Profile_requires_names_and_phone()
    {
        Assert.Throws<DomainException>(() => new Profile(1, "", "Soto", "+51 900", null, null, null));
        var profile = new Profile(1, " Camila ", "Soto", "+51 900 000 000", null, null, null);
        Assert.Equal("Camila", profile.FirstName);
        Assert.True(profile.IsOwnedBy(1));
    }
}
