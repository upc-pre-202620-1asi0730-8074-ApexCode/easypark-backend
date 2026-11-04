using EasyPark.Platform.Iam.Application.Internal.OutboundServices;
using BCryptNet = BCrypt.Net.BCrypt;

namespace EasyPark.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;

/// <summary>
///     BCrypt password hashing (work factor 11). Plain-text passwords are never stored.
/// </summary>
public class HashingService : IHashingService
{
    private const int WorkFactor = 11;

    public string HashPassword(string password)
    {
        return BCryptNet.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCryptNet.Verify(password, passwordHash);
        }
        catch (global::BCrypt.Net.SaltParseException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
