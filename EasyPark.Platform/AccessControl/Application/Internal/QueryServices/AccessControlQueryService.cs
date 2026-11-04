using EasyPark.Platform.AccessControl.Application.QueryServices;
using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.Queries;
using EasyPark.Platform.AccessControl.Domain.Repositories;
using EasyPark.Platform.ParkingManagement.Interfaces.ACL;
using EasyPark.Platform.Profiles.Interfaces.ACL;

namespace EasyPark.Platform.AccessControl.Application.Internal.QueryServices;

public class AccessControlQueryService(
    IAccessMovementRepository movementRepository,
    IParkingStayRepository stayRepository,
    IProfilesContextFacade profilesContextFacade,
    IParkingManagementContextFacade parkingContextFacade) : IAccessControlQueryService
{
    public async Task<IReadOnlyList<AccessMovement>> Handle(GetAccessMovementsQuery query,
        CancellationToken cancellationToken = default)
    {
        var facilityIds = await ManagedFacilityIdsAsync(query.OperatorUserAccountId, query.ParkingFacilityId,
            cancellationToken);
        var movements = await movementRepository.ListByFacilityIdsAsync(facilityIds, cancellationToken);
        return movements.OrderByDescending(movement => movement.OccurredAt).ToList();
    }

    public async Task<AccessMovement?> Handle(GetAccessMovementByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var movement = await movementRepository.FindByIdAsync(query.MovementId, cancellationToken);
        if (movement is null) return null;
        var facilityIds = await ManagedFacilityIdsAsync(query.OperatorUserAccountId, null, cancellationToken);
        return facilityIds.Contains(movement.ParkingFacilityId) ? movement : null;
    }

    public async Task<IReadOnlyList<ParkingStay>> Handle(GetParkingStaysQuery query,
        CancellationToken cancellationToken = default)
    {
        var facilityIds = await ManagedFacilityIdsAsync(query.OperatorUserAccountId, query.ParkingFacilityId,
            cancellationToken);
        var stays = await stayRepository.ListByFacilityIdsAsync(facilityIds, cancellationToken);
        return stays.Where(stay => !query.OnlyOpen || stay.IsOpen)
            .OrderByDescending(stay => stay.StartedAt)
            .ToList();
    }

    public async Task<ParkingStay?> Handle(GetParkingStayByIdQuery query, CancellationToken cancellationToken = default)
    {
        var stay = await stayRepository.FindByIdAsync(query.ParkingStayId, cancellationToken);
        if (stay is null) return null;
        var facilityIds = await ManagedFacilityIdsAsync(query.OperatorUserAccountId, null, cancellationToken);
        return facilityIds.Contains(stay.ParkingFacilityId) ? stay : null;
    }

    /// <summary>Facilities of the operator, optionally restricted to one of them.</summary>
    private async Task<List<int>> ManagedFacilityIdsAsync(int operatorUserAccountId, int? onlyFacilityId,
        CancellationToken cancellationToken)
    {
        var ownProfileId = await profilesContextFacade.FetchProfileIdByUserAccountIdAsync(operatorUserAccountId,
            cancellationToken);
        if (ownProfileId is null) return new List<int>();
        var ids = await parkingContextFacade.FetchFacilityIdsByOperatorProfileIdAsync(ownProfileId.Value,
            cancellationToken);
        return onlyFacilityId.HasValue
            ? ids.Where(id => id == onlyFacilityId.Value).ToList()
            : ids.ToList();
    }
}
