using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Domain.Model.ValueObjects;
using EasyPark.Platform.Iam.Domain.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Iam.Infrastructure.Persistence.EFC.Repositories;

public class UserAccountRepository(AppDbContext context)
    : BaseRepository<UserAccount>(context), IUserAccountRepository
{
    public async Task<UserAccount?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        return await Context.Set<UserAccount>().FirstOrDefaultAsync(account => account.Email == email.Value,
            cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        return await Context.Set<UserAccount>().AnyAsync(account => account.Email == email.Value, cancellationToken);
    }
}
