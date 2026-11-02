namespace EasyPark.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

/// <summary>
///     JWT settings bound from the <c>TokenSettings</c> section. In production the secret must come from the
///     environment variable <c>TokenSettings__Secret</c> (never from a committed file).
/// </summary>
public class TokenSettings
{
    public const string SectionName = "TokenSettings";
    public const int MinimumSecretLength = 32;

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "EasyPark.Platform";

    public string Audience { get; set; } = "EasyPark.WebApplication";

    public int ExpirationMinutes { get; set; } = 480;
}
