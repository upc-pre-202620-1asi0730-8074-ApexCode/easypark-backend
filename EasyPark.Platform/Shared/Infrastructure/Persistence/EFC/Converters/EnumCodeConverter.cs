using EasyPark.Platform.Shared.Domain.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Converters;

/// <summary>
///     Stores an enum as its contract code (for example <c>OPERATOR_ADMIN</c>) so that the values seen in
///     MySQL Workbench are the same values that the REST API returns.
/// </summary>
public class EnumCodeConverter<TEnum> : ValueConverter<TEnum, string> where TEnum : struct, Enum
{
    public EnumCodeConverter() : base(
        value => EnumCodes.ToCode(value),
        code => EnumCodes.FromDatabase<TEnum>(code))
    {
    }
}
