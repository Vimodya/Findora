namespace Findora.API.Models;

/// <summary>
/// Canonical role name constants, shared by seeding (<see cref="FindoraDbContext"/>),
/// registration (default role assignment), and <c>[Authorize(Roles = ...)]</c>
/// checks — so role names are never repeated as magic strings.
/// </summary>
public static class RoleNames
{
    public const string User = "User";
    public const string OrganizationStaff = "OrganizationStaff";
    public const string Admin = "Admin";

    /// <summary>All roles seeded at startup, in seed order.</summary>
    public static readonly IReadOnlyList<string> All = new[] { User, OrganizationStaff, Admin };
}
