namespace EasyPark.Platform.AccessControl.Domain.Model.Queries;

public record GetAccessMovementsQuery(int OperatorUserAccountId, int? ParkingFacilityId);

public record GetAccessMovementByIdQuery(int MovementId, int OperatorUserAccountId);

public record GetParkingStaysQuery(int OperatorUserAccountId, int? ParkingFacilityId, bool OnlyOpen);

public record GetParkingStayByIdQuery(int ParkingStayId, int OperatorUserAccountId);
