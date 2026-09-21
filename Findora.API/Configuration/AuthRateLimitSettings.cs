namespace Findora.API.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>"RateLimiting:Auth"</c> section,
/// controlling the fixed-window limiter applied to the auth-sensitive
/// endpoints (register/login/refresh/forgot-password/etc. — see
/// <c>Program.cs</c> and <c>AuthController</c>). Not a secret; safe to
/// commit sensible defaults.
/// </summary>
public class AuthRateLimitSettings
{
    public const string SectionName = "RateLimiting:Auth";

    /// <summary>Requests permitted per client (IP) per window.</summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>Window length, in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;
}
