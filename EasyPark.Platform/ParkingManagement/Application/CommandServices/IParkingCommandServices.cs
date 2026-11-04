using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.Commands;

namespace EasyPark.Platform.ParkingManagement.Application.CommandServices;

public interface IParkingFacilityCommandService
{
    Task<ParkingFacility> Handle(CreateParkingFacilityCommand command, CancellationToken cancellationToken = default);

    Task<ParkingFacility> Handle(UpdateParkingFacilityCommand command, CancellationToken cancellationToken = default);
}

public interface IParkingSpotCommandService
{
    Task<ParkingSpot> Handle(CreateParkingSpotCommand command, CancellationToken cancellationToken = default);

    Task<ParkingSpot> Handle(UpdateParkingSpotCommand command, CancellationToken cancellationToken = default);
}