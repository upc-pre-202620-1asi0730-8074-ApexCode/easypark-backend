using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Profiles.Domain.Repositories;

public interface IProfileRepository : IBaseRepository<Profile>
{
    Task<Profile?> FindByUserAccountIdAsync(int userAccountId, CancellationToken cancellationToken = default);
}
