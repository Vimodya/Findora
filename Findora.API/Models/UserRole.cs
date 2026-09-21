namespace Findora.API.Models;

/// <summary>
/// Join entity for the many-to-many User &lt;-&gt; Role relationship.
/// Composite key (UserId, RoleId) configured in <c>FindoraDbContext</c>.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
