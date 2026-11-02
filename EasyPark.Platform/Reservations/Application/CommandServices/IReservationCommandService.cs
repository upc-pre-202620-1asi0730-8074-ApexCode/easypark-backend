using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.Commands;

namespace EasyPark.Platform.Reservations.Application.CommandServices;

public interface IReservationCommandService
{
    Task<Reservation> Handle(CreateReservationCommand command, CancellationToken cancellationToken = default);

    Task<Reservation> Handle(CancelReservationCommand command, CancellationToken cancellationToken = default);

    /// <summary>Expires overdue reservations and frees their spots. Returns how many were expired.</summary>
    Task<int> Handle(ExpireOverdueReservationsCommand command, CancellationToken cancellationToken = default);
}
