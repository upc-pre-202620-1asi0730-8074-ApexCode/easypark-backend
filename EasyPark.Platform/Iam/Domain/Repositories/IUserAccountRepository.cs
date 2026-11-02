using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Iam.Domain.Repositories;

public interface IUserAccountRepository : IBaseRepository<UserAccount>
{
    Task<UserAccount?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);
}
