using EasyPark.Platform.Iam.Domain.Model.Aggregates;

namespace EasyPark.Platform.Iam.Application.Internal.OutboundServices;

public interface ITokenService
{
    string GenerateToken(UserAccount userAccount);
}
