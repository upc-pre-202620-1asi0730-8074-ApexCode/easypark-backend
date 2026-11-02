using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.Commands;

namespace EasyPark.Platform.AccessControl.Application.CommandServices;

/// <summary>Result of registering an access: the movement and, for a completed entry or exit, the stay.</summary>
public record AccessRegistrationResult(AccessMovement Movement, ParkingStay? ParkingStay);

public interface IAccessControlCommandService
{
    Task<AccessRegistrationResult> Handle(RegisterAccessCommand command, CancellationToken cancellationToken = default);

    Task<AccessMovement> Handle(RejectAccessMovementCommand command, CancellationToken cancellationToken = default);

    Task<ParkingStay> Handle(ReassignParkingSpotCommand command, CancellationToken cancellationToken = default);
}
