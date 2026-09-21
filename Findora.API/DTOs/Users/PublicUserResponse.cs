namespace Findora.API.DTOs.Users;

/// <summary>
/// Shape returned by <c>GET /api/v1/users/{id}</c> — deliberately minimal.
/// No email, phone, status, reputation, or role list: none of that is
/// "necessary" public contact/trust information at this stage, and
/// exposing it would leak more than a stranger looking up a user needs.
/// Never derived from anything containing <c>PasswordHash</c> or token data.
/// </summary>
public class PublicUserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    /// <summary>When the account was created ("member since").</summary>
    public DateTime MemberSince { get; set; }
}
