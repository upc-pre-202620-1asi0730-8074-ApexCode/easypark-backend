using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Profiles.Infrastructure.Persistence.EFC.Repositories;

public class ProfileRepository(AppDbContext context) : BaseRepository<Profile>(context), IProfileRepository
{
    public async Task<Profile?> FindByUserAccountIdAsync(int userAccountId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Profile>()
            .FirstOrDefaultAsync(profile => profile.UserAccountId == userAccountId, cancellationToken);
    }
}
