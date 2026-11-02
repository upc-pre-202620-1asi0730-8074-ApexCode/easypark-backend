using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Repositories;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.Profiles.Infrastructure.Persistence.EFC.Repositories;

public class VehicleRepository(AppDbContext context) : BaseRepository<Vehicle>(context), IVehicleRepository
{
    public async Task<Vehicle?> FindByPlateNumberAsync(string plateNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Vehicle>()
            .FirstOrDefaultAsync(vehicle => vehicle.PlateNumber == plateNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> ListByProfileIdAsync(int profileId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Vehicle>()
            .Where(vehicle => vehicle.ProfileId == profileId)
            .OrderBy(vehicle => vehicle.Id)
            .ToListAsync(cancellationToken);
    }
}
