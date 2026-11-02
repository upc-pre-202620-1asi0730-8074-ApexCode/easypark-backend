namespace EasyPark.Platform.Profiles.Domain.Model.Queries;

public record GetProfileByUserAccountIdQuery(int UserAccountId);

public record GetProfileByIdQuery(int ProfileId);

public record GetVehiclesByProfileIdQuery(int ProfileId);

public record GetVehicleByPlateNumberQuery(string PlateNumber);

public record GetVehicleByIdQuery(int VehicleId);
