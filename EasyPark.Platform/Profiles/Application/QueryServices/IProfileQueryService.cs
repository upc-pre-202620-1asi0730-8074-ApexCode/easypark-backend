using EasyPark.Platform.Profiles.Domain.Model.Aggregates;
using EasyPark.Platform.Profiles.Domain.Model.Queries;

namespace EasyPark.Platform.Profiles.Application.QueryServices;

public interface IProfileQueryService
{
    Task<Profile?> Handle(GetProfileByUserAccountIdQuery query, CancellationToken cancellationToken = default);

    Task<Profile?> Handle(GetProfileByIdQuery query, CancellationToken cancellationToken = default);
}
