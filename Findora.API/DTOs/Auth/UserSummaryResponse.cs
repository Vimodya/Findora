namespace Findora.API.DTOs.Auth;

/// <summary>
/// Public-facing shape of a <c>Models.User</c>. Controllers never return the
/// entity itself — this omits <c>PasswordHash</c>, navigation properties,
/// and audit/soft-delete fields that callers have no business seeing.
/// </summary>
public class UserSummaryResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
