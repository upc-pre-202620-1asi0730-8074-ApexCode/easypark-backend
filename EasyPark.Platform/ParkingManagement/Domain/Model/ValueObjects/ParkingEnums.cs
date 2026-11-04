namespace EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;

/// <summary>Contract codes: ACTIVE, INACTIVE, MAINTENANCE.</summary>
public enum FacilityStatus
{
    Active,
    Inactive,
    Maintenance
}

/// <summary>Contract codes: CAR, SUV, VAN, MOTORCYCLE (same codes as the vehicle types).</summary>
public enum SpotType
{
    Car,
    Suv,
    Van,
    Motorcycle
}

/// <summary>Contract codes: AVAILABLE, OCCUPIED, RESERVED, OUT_OF_SERVICE.</summary>
public enum SpotStatus
{
    Available,
    Occupied,
    Reserved,
    OutOfService
}