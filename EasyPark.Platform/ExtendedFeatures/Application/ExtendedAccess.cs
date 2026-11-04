using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Application;

/// <summary>Authorization boundary shared by the new contexts. Data is always scoped to the operator's facilities.</summary>
public sealed class ExtendedAccess(AppDbContext db)
{
    public async Task<int[]> FacilityIdsAsync(int userAccountId, CancellationToken ct) =>
        await (from facility in db.Set<ParkingFacility>()
               join profile in db.Set<Profile>() on facility.OperatorProfileId equals profile.Id
               where profile.UserAccountId == userAccountId
               select facility.Id).ToArrayAsync(ct);

    public async Task AssertFacilityAsync(int facilityId, int userAccountId, CancellationToken ct)
    {
        if (!(await FacilityIdsAsync(userAccountId, ct)).Contains(facilityId))
            throw DomainException.Forbidden("facility-forbidden", "You do not manage this facility.");
    }

    public static T Existing<T>(T? entity, string resource) where T : class =>
        entity ?? throw DomainException.NotFound("not-found", resource + " was not found.");
}
