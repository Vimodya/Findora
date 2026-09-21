namespace Findora.API.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>"Cors"</c> configuration section, so
/// allowed cross-origin hosts can be set per environment without any code
/// change — e.g. the local React dev server in Development, a real deployed
/// frontend domain later in Production.
/// </summary>
public class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Origins allowed to make cross-origin requests to the API (e.g.
    /// <c>"http://localhost:5173"</c>). Empty by default — no cross-origin
    /// access — until explicitly configured per environment. Never use a
    /// wildcard ("*") origin; that is not safe once cookies/credentials are
    /// involved (Module 2 auth) and is never appropriate for production.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
