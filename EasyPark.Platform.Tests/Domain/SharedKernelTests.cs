using EasyPark.Platform.AccessControl.Domain.Model.ValueObjects;
using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration.Extensions;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace EasyPark.Platform.Tests.Domain;

public class SharedKernelTests
{
    [Theory]
    [InlineData(UserRole.OperatorAdmin, "OPERATOR_ADMIN")]
    [InlineData(UserRole.Driver, "DRIVER")]
    [InlineData(UserRole.OperatorStaff, "OPERATOR_STAFF")]
    public void EnumCodes_converts_user_roles_to_contract_codes(UserRole role, string expected)
    {
        Assert.Equal(expected, EnumCodes.ToCode(role));
        Assert.True(EnumCodes.TryParse<UserRole>(expected, out var parsed));
        Assert.Equal(role, parsed);
    }

    [Fact]
    public void EnumCodes_converts_multi_word_members()
    {
        Assert.Equal("OUT_OF_SERVICE", EnumCodes.ToCode(SpotStatus.OutOfService));
        Assert.Equal("QR_CODE", EnumCodes.ToCode(RegistrationMethod.QrCode));
        Assert.Equal("IOT_SENSOR", EnumCodes.ToCode(RegistrationMethod.IotSensor));
        Assert.Equal("UNDER_REVIEW", EnumCodes.ToCode(MovementStatus.UnderReview));
        Assert.Equal("SUV", EnumCodes.ToCode(SpotType.Suv));
    }

    [Fact]
    public void EnumCodes_parse_is_case_insensitive_and_rejects_unknown_values()
    {
        Assert.Equal(SpotType.Motorcycle, EnumCodes.Parse<SpotType>("motorcycle", "x", "spotType"));
        var exception = Assert.Throws<DomainException>(() => EnumCodes.Parse<SpotType>("TRUCK", "invalid", "spotType"));
        Assert.Equal(DomainErrorKind.BadRequest, exception.Kind);
        Assert.Equal("invalid", exception.Code);
    }

    [Theory]
    [InlineData("UserAccountId", "user_account_id")]
    [InlineData("Id", "id")]
    [InlineData("IsDefault", "is_default")]
    [InlineData("EntryMovementId", "entry_movement_id")]
    public void Column_names_use_snake_case(string property, string expected)
    {
        Assert.Equal(expected, ModelBuilderExtensions.ToSnakeCase(property));
    }

    [Theory]
    [InlineData("ParkingFacilities", "parking-facilities")]
    [InlineData("AccessMovements", "access-movements")]
    [InlineData("Authentication", "authentication")]
    public void Routes_use_kebab_case(string controllerName, string expected)
    {
        Assert.Equal(expected, controllerName.ToKebabCase());
    }

    [Fact]
    public void Guard_converts_unspecified_dates_to_utc()
    {
        var value = Guard.ToUtc(new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Unspecified));
        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(10, value.Hour);
    }
}
