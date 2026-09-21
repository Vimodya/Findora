namespace Findora.API.Models;

/// <summary>
/// An authorization role (<see cref="RoleNames"/>). Seeded once via
/// <c>FindoraDbContext.OnModelCreating</c> — not created/edited through the
/// API in Module 2.
/// </summary>
public class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
