namespace Findora.API.Models;

/// <summary>
/// A single-use, expiring token proving control of a user's email address.
/// Only the SHA-256 hash is persisted (see <c>Services/OpaqueTokenGenerator.cs</c>).
/// </summary>
public class EmailVerificationToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public bool IsValid(DateTime utcNow) => UsedAt is null && ExpiresAt > utcNow;
}
