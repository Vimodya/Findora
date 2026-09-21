namespace Findora.API.Models;

/// <summary>
/// A single-use, expiring token authorizing a password reset. Only the
/// SHA-256 hash is persisted (see <c>Services/OpaqueTokenGenerator.cs</c>).
/// </summary>
public class PasswordResetToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public bool IsValid(DateTime utcNow) => UsedAt is null && ExpiresAt > utcNow;
}
