using EasyPark.Platform.Iam.Application.QueryServices;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.Queries;
using EasyPark.Platform.Iam.Domain.Repositories;

namespace EasyPark.Platform.Iam.Application.Internal.QueryServices;

public class UserAccountQueryService(IUserAccountRepository userAccountRepository) : IUserAccountQueryService
{
    public async Task<UserAccount?> Handle(GetUserAccountByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await userAccountRepository.FindByIdAsync(query.Id, cancellationToken);
    }
}
