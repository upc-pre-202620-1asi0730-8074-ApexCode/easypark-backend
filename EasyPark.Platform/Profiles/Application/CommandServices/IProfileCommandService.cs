using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Commands;

namespace EasyPark.Platform.Profiles.Application.CommandServices;

public interface IProfileCommandService
{
    Task<Profile> Handle(CreateProfileCommand command, CancellationToken cancellationToken = default);

    Task<Profile> Handle(UpdateProfileCommand command, CancellationToken cancellationToken = default);
}
