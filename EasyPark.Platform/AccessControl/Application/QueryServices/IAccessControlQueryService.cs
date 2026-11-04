using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.Queries;

namespace EasyPark.Platform.AccessControl.Application.QueryServices;

public interface IAccessControlQueryService
{
    Task<IReadOnlyList<AccessMovement>> Handle(GetAccessMovementsQuery query, CancellationToken cancellationToken = default);

    Task<AccessMovement?> Handle(GetAccessMovementByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingStay>> Handle(GetParkingStaysQuery query, CancellationToken cancellationToken = default);

    Task<ParkingStay?> Handle(GetParkingStayByIdQuery query, CancellationToken cancellationToken = default);
}
