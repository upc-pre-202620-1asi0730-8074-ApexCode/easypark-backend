namespace EasyPark.Platform.AccessControl.Interfaces.ACL;

/// <summary>Anti-corruption layer exposed by Access Control to the other bounded contexts.</summary>
public interface IAccessControlContextFacade
{
    Task<bool> HasOpenStayOnSpotAsync(int parkingSpotId, CancellationToken cancellationToken = default);
}
