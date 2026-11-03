using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.ParkingManagement.Domain.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ParkingManagement.Infrastructure.Persistence.EFC.Repositories;

public class ParkingFacilityRepository(AppDbContext context)
    : BaseRepository<ParkingFacility>(context), IParkingFacilityRepository
{
    public async Task<IReadOnlyList<ParkingFacility>> ListByOperatorProfileIdAsync(int operatorProfileId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ParkingFacility>()
            .Where(facility => facility.OperatorProfileId == operatorProfileId)
            .OrderBy(facility => facility.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingFacility>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<ParkingFacility>()
            .Where(facility => facility.Status == FacilityStatus.Active)
            .OrderBy(facility => facility.Id)
            .ToListAsync(cancellationToken);
    }
}

public class ParkingSpotRepository(AppDbContext context)
    : BaseRepository<ParkingSpot>(context), IParkingSpotRepository
{
    public async Task<IReadOnlyList<ParkingSpot>> ListByFacilityIdAsync(int facilityId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ParkingSpot>()
            .Where(spot => spot.FacilityId == facilityId)
            .OrderBy(spot => spot.Level).ThenBy(spot => spot.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingSpot>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default)
    {
        if (facilityIds.Count == 0) return new List<ParkingSpot>();
        var ids = facilityIds.ToList();
        return await Context.Set<ParkingSpot>()
            .Where(spot => ids.Contains(spot.FacilityId))
            .OrderBy(spot => spot.FacilityId).ThenBy(spot => spot.Level).ThenBy(spot => spot.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsCodeInFacilityAsync(int facilityId, string code, int? excludedSpotId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ParkingSpot>().AnyAsync(spot =>
            spot.FacilityId == facilityId && spot.Code == code &&
            (!excludedSpotId.HasValue || spot.Id != excludedSpotId.Value), cancellationToken);
    }

    public async Task<ParkingSpot?> FindFirstAvailableAsync(int facilityId, SpotType spotType,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ParkingSpot>()
            .Where(spot => spot.FacilityId == facilityId && spot.SpotType == spotType &&
                           spot.Status == SpotStatus.Available)
            .OrderBy(spot => spot.Level).ThenBy(spot => spot.Code)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
