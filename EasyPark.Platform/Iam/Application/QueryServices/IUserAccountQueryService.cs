using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.Queries;

namespace EasyPark.Platform.Iam.Application.QueryServices;

public interface IUserAccountQueryService
{
    Task<UserAccount?> Handle(GetUserAccountByIdQuery query, CancellationToken cancellationToken = default);
}
