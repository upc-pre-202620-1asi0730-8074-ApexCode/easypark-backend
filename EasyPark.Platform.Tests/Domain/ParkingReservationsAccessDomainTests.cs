using EasyPark.Platform.AccessControl.Domain.Model.Aggregates;
using EasyPark.Platform.AccessControl.Domain.Model.ValueObjects;
using EasyPark.Platform.ParkingManagement.Domain.Model.Aggregates;
using EasyPark.Platform.ParkingManagement.Domain.Model.ValueObjects;
using EasyPark.Platform.Reservations.Domain.Model.Aggregates;
using EasyPark.Platform.Reservations.Domain.Model.ValueObjects;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;

namespace EasyPark.Platform.Tests.Domain;

public class ParkingReservationsAccessDomainTests
{
    [Fact]
    public void Spot_life_cycle_reserve_occupy_release()
    {
        var spot = new ParkingSpot(1, " a-01 ", 1, SpotType.Car);
        Assert.Equal("A-01", spot.Code);
        Assert.True(spot.IsAvailable);
        var initialVersion = spot.Version;

        spot.Reserve();
        Assert.Equal(SpotStatus.Reserved, spot.Status);
        Assert.Throws<DomainException>(() => spot.Reserve());

        spot.Occupy();
        Assert.Equal(SpotStatus.Occupied, spot.Status);
        Assert.Throws<DomainException>(() => spot.Occupy());

        spot.Release();
        Assert.True(spot.IsAvailable);
        Assert.True(spot.Version > initialVersion);
    }

    [Fact]
    public void Out_of_service_spot_cannot_be_occupied_until_returned()
    {
        var spot = new ParkingSpot(1, "B-01", 2, SpotType.Suv);
        spot.MarkOutOfService();
        Assert.Throws<DomainException>(() => spot.Occupy());
        Assert.Throws<DomainException>(() => spot.Reserve());
        spot.ReturnToService();
        Assert.True(spot.IsAvailable);
        Assert.Throws<DomainException>(() => spot.ReturnToService());
    }

    [Fact]
    public void Facility_validates_rate_coordinates_and_hours()
    {
        Assert.Throws<DomainException>(() => new ParkingFacility(1, "N", "A", null, null, -1m, null, null,
            FacilityStatus.Active));
        Assert.Throws<DomainException>(() => new ParkingFacility(1, "N", "A", 120m, 0m, 5m, null, null,
            FacilityStatus.Active));
        Assert.Throws<DomainException>(() => new ParkingFacility(1, "N", "A", null, null, 5m, "25:00", null,
            FacilityStatus.Active));

        var facility = new ParkingFacility(1, "Centro", "Av. Larco 345", -12.1215m, -77.0297m, 6.5m, "06:00", "23:00",
            FacilityStatus.Active);
        Assert.True(facility.IsActive);
        Assert.Equal("06:00", facility.OpenTime);
        Assert.True(facility.IsManagedBy(1));
        var distance = facility.DistanceInKilometresTo(-12.1215, -77.0297);
        Assert.NotNull(distance);
        Assert.True(distance < 0.01);
    }

    [Fact]
    public void Reservation_amount_is_hourly_rate_times_duration()
    {
        Assert.Equal(9.75m, Reservation.CalculateAmount(6.5m, 90));
        Assert.Equal(13.00m, Reservation.CalculateAmount(6.5m, 120));
    }

    [Fact]
    public void Reservation_life_cycle_and_invalid_transitions()
    {
        var start = DateTime.UtcNow.AddHours(1);
        var reservation = new Reservation(ReservationCode.Generate(), 1, 2, 3, 4, start, 60, 6.5m);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(6.5m, reservation.EstimatedAmount);
        Assert.Equal(start.AddMinutes(60), reservation.EndAt);
        Assert.StartsWith("EP-", reservation.Code);

        Assert.Throws<DomainException>(() => reservation.Complete());
        reservation.Activate();
        var exception = Assert.Throws<DomainException>(() => reservation.Cancel());
        Assert.Equal("not-cancellable", exception.Code);
        reservation.Complete();
        Assert.Equal(ReservationStatus.Completed, reservation.Status);
    }

    [Fact]
    public void Reservation_duration_is_validated()
    {
        Assert.Throws<DomainException>(() => new Reservation("EP-TEST0001", 1, 2, 3, 4, DateTime.UtcNow, 5, 1m));
        Assert.Throws<DomainException>(() => new Reservation("EP-TEST0001", 1, 2, 3, 4, DateTime.UtcNow, 2000, 1m));
    }

    [Fact]
    public void Reservation_overlap_and_overdue_detection()
    {
        var start = new DateTime(2026, 10, 25, 15, 0, 0, DateTimeKind.Utc);
        var reservation = new Reservation("EP-TEST0002", 1, 2, 3, 4, start, 60, 1m);
        Assert.True(reservation.Overlaps(start.AddMinutes(30), start.AddMinutes(90)));
        Assert.False(reservation.Overlaps(start.AddMinutes(60), start.AddMinutes(120)));
        Assert.False(reservation.IsOverdue(start.AddMinutes(59), 0));
        Assert.True(reservation.IsOverdue(start.AddMinutes(61), 0));
        reservation.Expire();
        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.False(reservation.IsOverdue(start.AddDays(1), 0));
    }

    [Fact]
    public void Parking_stay_charges_whole_minutes_and_closes_once()
    {
        var start = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc);
        var stay = new ParkingStay(1, 2, 3, 4, 5, start);
        Assert.True(stay.IsOpen);
        stay.Close(6, start.AddMinutes(90).AddSeconds(59), 6.5m);
        Assert.False(stay.IsOpen);
        Assert.Equal(9.75m, stay.ChargedAmount);
        Assert.Throws<DomainException>(() => stay.Close(7, start.AddHours(3), 6.5m));
        Assert.Throws<DomainException>(() => stay.ReassignSpot(9));
    }

    [Fact]
    public void Access_movements_keep_status_and_reason()
    {
        var rejected = AccessMovement.Rejected(1, 2, "ABC-123", 3, MovementType.Entry, RegistrationMethod.Manual,
            AccessRejectionReasons.NoActiveReservation);
        Assert.Equal(MovementStatus.Rejected, rejected.Status);
        Assert.Equal("no-active-reservation", rejected.Note);

        var review = AccessMovement.UnderReview(1, 2, "ABC-123", 3, MovementType.Entry, RegistrationMethod.QrCode,
            AccessRejectionReasons.VehicleUnderReview);
        review.RejectReview("plate not recognized");
        Assert.Equal(MovementStatus.Rejected, review.Status);
        Assert.Throws<DomainException>(() => review.RejectReview(null));
    }
}
