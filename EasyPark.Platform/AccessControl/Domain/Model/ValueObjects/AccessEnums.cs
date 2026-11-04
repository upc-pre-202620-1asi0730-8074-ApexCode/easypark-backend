namespace EasyPark.Platform.AccessControl.Domain.Model.ValueObjects;

/// <summary>Contract codes: ENTRY, EXIT.</summary>
public enum MovementType
{
    Entry,
    Exit
}

/// <summary>Contract codes: QR_CODE, MANUAL, IOT_SENSOR.</summary>
public enum RegistrationMethod
{
    QrCode,
    Manual,
    IotSensor
}

/// <summary>Contract codes: IN_PROGRESS, COMPLETED, UNDER_REVIEW, REJECTED.</summary>
public enum MovementStatus
{
    InProgress,
    Completed,
    UnderReview,
    Rejected
}

/// <summary>
///     Reasons stored in <c>note</c> when a movement is rejected or sent to review. The Web Application
///     translates them (access-control.errors.*).
/// </summary>
public static class AccessRejectionReasons
{
    public const string VehicleUnderReview = "vehicle-under-review";
    public const string VehicleAlreadyInside = "vehicle-already-inside";
    public const string NoActiveReservation = "no-active-reservation";
    public const string NoAvailability = "no-availability";
    public const string NoActiveStay = "no-active-stay";
}
