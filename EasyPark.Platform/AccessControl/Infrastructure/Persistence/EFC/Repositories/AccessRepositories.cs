using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.AccessControl.Infrastructure.Persistence.EFC.Repositories;

public class AccessMovementRepository(AppDbContext context)
    : BaseRepository<AccessMovement>(context), IAccessMovementRepository
{
    public async Task<IReadOnlyList<AccessMovement>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default)
    {
        if (facilityIds.Count == 0) return new List<AccessMovement>();
        var ids = facilityIds.ToList();
        return await Context.Set<AccessMovement>()
            .Where(movement => ids.Contains(movement.ParkingFacilityId))
            .OrderByDescending(movement => movement.OccurredAt)
            .ToListAsync(cancellationToken);
    }
}

public class ParkingStayRepository(AppDbContext context)
    : BaseRepository<ParkingStay>(context), IParkingStayRepository
{
    public async Task<ParkingStay?> FindOpenByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<ParkingStay>()
            .FirstOrDefaultAsync(stay => stay.VehicleId == vehicleId && stay.ExitMovementId == null, cancellationToken);
    }

    public async Task<bool> ExistsOpenBySpotIdAsync(int parkingSpotId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<ParkingStay>()
            .AnyAsync(stay => stay.ParkingSpotId == parkingSpotId && stay.ExitMovementId == null, cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingStay>> ListByFacilityIdsAsync(IReadOnlyCollection<int> facilityIds,
        CancellationToken cancellationToken = default)
    {
        if (facilityIds.Count == 0) return new List<ParkingStay>();
        var ids = facilityIds.ToList();
        return await Context.Set<ParkingStay>()
            .Where(stay => ids.Contains(stay.ParkingFacilityId))
            .OrderByDescending(stay => stay.StartedAt)
            .ToListAsync(cancellationToken);
    }
}
