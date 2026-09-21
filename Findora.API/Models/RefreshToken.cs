namespace Findora.API.Models;

/// <summary>
/// A single refresh-token "session" for a user. Only a SHA-256 hash of the
/// opaque token is ever stored (see <c>Services/OpaqueTokenGenerator.cs</c>)
/// — the raw token exists only transiently, on the wire to the client.
/// Rotation: each successful <c>/api/v1/auth/refresh</c> call revokes the
/// presented token and issues a brand new one (<see cref="ReplacedByTokenHash"/>
/// links the two for audit/reuse-detection purposes).
/// </summary>
public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    /// <summary>Hash of the token this one was rotated into, if any.</summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>Best-effort client IP at issuance, for audit purposes only.</summary>
    public string? CreatedByIp { get; set; }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    public bool IsActive(DateTime utcNow) => RevokedAt is null && !IsExpired(utcNow);
}
