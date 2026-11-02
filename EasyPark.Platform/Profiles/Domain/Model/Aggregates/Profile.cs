using EasyPark.Platform.Shared.Domain.Model;

namespace EasyPark.Platform.Profiles.Domain.Model.Aggregates;

/// <summary>
///     Personal profile of a user account (driver or parking operator). One profile per user account.
///     Company name and job title only apply to operator profiles.
/// </summary>
public class Profile
{
    /// <summary>Constructor required by Entity Framework Core.</summary>
    protected Profile()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Phone = string.Empty;
    }

    public Profile(int userAccountId, string? firstName, string? lastName, string? phone, string? photoUrl,
        string? companyName, string? jobTitle)
    {
        if (userAccountId <= 0) throw new ArgumentOutOfRangeException(nameof(userAccountId));
        UserAccountId = userAccountId;
        FirstName = string.Empty;
        LastName = string.Empty;
        Phone = string.Empty;
        UpdateDetails(firstName, lastName, phone, photoUrl, companyName, jobTitle);
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public int UserAccountId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Phone { get; private set; }

    public string? PhotoUrl { get; private set; }

    public string? CompanyName { get; private set; }

    public string? JobTitle { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsOwnedBy(int userAccountId)
    {
        return UserAccountId == userAccountId;
    }

    public void UpdateDetails(string? firstName, string? lastName, string? phone, string? photoUrl,
        string? companyName, string? jobTitle)
    {
        FirstName = Guard.RequiredText(firstName, 100, "invalid-profile", "firstName");
        LastName = Guard.RequiredText(lastName, 120, "invalid-profile", "lastName");
        Phone = Guard.RequiredText(phone, 40, "invalid-profile", "phone");
        PhotoUrl = Guard.OptionalText(photoUrl, 1000, "invalid-profile", "photoUrl");
        CompanyName = Guard.OptionalText(companyName, 180, "invalid-profile", "companyName");
        JobTitle = Guard.OptionalText(jobTitle, 120, "invalid-profile", "jobTitle");
    }
}
