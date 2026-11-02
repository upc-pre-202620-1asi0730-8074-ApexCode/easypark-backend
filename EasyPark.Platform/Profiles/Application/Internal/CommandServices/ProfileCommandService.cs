using EasyPark.Platform.Profiles.Application.CommandServices;
using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Commands;
using EasyPark.Platform.Profiles.Domain.Repositories;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Domain.Repositories;

namespace EasyPark.Platform.Profiles.Application.Internal.CommandServices;

public class ProfileCommandService(IProfileRepository profileRepository, IUnitOfWork unitOfWork)
    : IProfileCommandService
{
    public async Task<Profile> Handle(CreateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = command.UserAccountId ?? command.RequesterUserAccountId;
        if (userAccountId != command.RequesterUserAccountId)
            throw DomainException.Forbidden("forbidden", "A user can only create their own profile.");

        if (await profileRepository.FindByUserAccountIdAsync(userAccountId, cancellationToken) is not null)
            throw DomainException.Conflict("profile-exists", "A profile already exists for this user account.");

        var profile = new Profile(userAccountId, command.FirstName, command.LastName, command.Phone,
            command.PhotoUrl,
            command.RequesterIsOperator ? command.CompanyName : null,
            command.RequesterIsOperator ? command.JobTitle : null);
        await profileRepository.AddAsync(profile, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return profile;
    }

    public async Task<Profile> Handle(UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.FindByIdAsync(command.ProfileId, cancellationToken)
                      ?? throw DomainException.NotFound("profile-not-found", "Profile not found.");
        if (!profile.IsOwnedBy(command.RequesterUserAccountId))
            throw DomainException.Forbidden("forbidden", "A user can only update their own profile.");

        profile.UpdateDetails(command.FirstName, command.LastName, command.Phone, command.PhotoUrl,
            command.RequesterIsOperator ? command.CompanyName : null,
            command.RequesterIsOperator ? command.JobTitle : null);
        await unitOfWork.CompleteAsync(cancellationToken);
        return profile;
    }
}
