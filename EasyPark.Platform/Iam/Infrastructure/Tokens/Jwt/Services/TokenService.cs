using System.Security.Claims;
using System.Text;
using EasyPark.Platform.Iam.Application.Internal.OutboundServices;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using EasyPark.Platform.Shared.Domain.Model;
using EasyPark.Platform.Shared.Interfaces.REST;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EasyPark.Platform.Iam.Infrastructure.Tokens.Jwt.Services;

/// <summary>
///     Issues HS256 JSON Web Tokens with the claims <c>sub</c> (user account id), <c>email</c> and <c>role</c>.
/// </summary>
public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
    private readonly TokenSettings _settings = tokenSettings.Value;

    public string GenerateToken(UserAccount userAccount)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimsPrincipalExtensions.SubjectClaim, userAccount.Id.ToString()),
                new Claim(ClaimsPrincipalExtensions.EmailClaim, userAccount.Email),
                new Claim(ClaimsPrincipalExtensions.RoleClaim, EnumCodes.ToCode(userAccount.Role)),
                new Claim("jti", Guid.NewGuid().ToString("N"))
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_settings.ExpirationMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
