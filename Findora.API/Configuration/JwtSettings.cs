namespace Findora.API.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>"Jwt"</c> configuration section.
/// <see cref="SigningKey"/> ships empty in every committed appsettings file
/// — it must be supplied via the <c>Jwt__SigningKey</c> environment
/// variable or <c>dotnet user-secrets</c> for local development (see
/// README), and via a real secrets manager in any deployed environment.
/// Never commit a real signing key.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum signing-key length (bytes) accepted at startup.</summary>
    public const int MinimumSigningKeyLength = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
