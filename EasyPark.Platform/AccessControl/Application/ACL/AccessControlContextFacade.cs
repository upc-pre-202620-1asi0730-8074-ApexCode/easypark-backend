using EasyPark.Platform.AccessControl.Domain.Repositories;
using EasyPark.Platform.AccessControl.Interfaces.ACL;

namespace EasyPark.Platform.AccessControl.Application.ACL;

public class AccessControlContextFacade(IParkingStayRepository stayRepository) : IAccessControlContextFacade
{
    public async Task<bool> HasOpenStayOnSpotAsync(int parkingSpotId, CancellationToken cancellationToken = default)
    {
        return await stayRepository.ExistsOpenBySpotIdAsync(parkingSpotId, cancellationToken);
    }
}
