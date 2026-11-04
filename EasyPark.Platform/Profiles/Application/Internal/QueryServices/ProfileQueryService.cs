using EasyPark.Platform.Profiles.Application.QueryServices;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Queries;
using EasyPark.Platform.Profiles.Domain.Repositories;

namespace EasyPark.Platform.Profiles.Application.Internal.QueryServices;

public class ProfileQueryService(IProfileRepository profileRepository) : IProfileQueryService
{
    public async Task<Profile?> Handle(GetProfileByUserAccountIdQuery query, CancellationToken cancellationToken = default)
    {
        return await profileRepository.FindByUserAccountIdAsync(query.UserAccountId, cancellationToken);
    }

    public async Task<Profile?> Handle(GetProfileByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await profileRepository.FindByIdAsync(query.ProfileId, cancellationToken);
    }
}
