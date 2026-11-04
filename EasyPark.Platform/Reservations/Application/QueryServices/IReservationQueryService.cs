using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Queries;

namespace EasyPark.Platform.Reservations.Application.QueryServices;

public interface IReservationQueryService
{
    Task<IReadOnlyList<Reservation>> Handle(GetReservationsQuery query, CancellationToken cancellationToken = default);

    Task<Reservation?> Handle(GetReservationByIdQuery query, CancellationToken cancellationToken = default);
}
