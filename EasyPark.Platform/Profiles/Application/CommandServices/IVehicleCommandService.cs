using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Commands;

namespace EasyPark.Platform.Profiles.Application.CommandServices;

public interface IVehicleCommandService
{
    Task<Vehicle> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken = default);

    Task<Vehicle> Handle(UpdateVehicleCommand command, CancellationToken cancellationToken = default);
}
