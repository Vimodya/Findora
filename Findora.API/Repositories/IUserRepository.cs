using Findora.API.Models;

namespace Findora.API.Repositories;

/// <summary>
/// Data-access abstraction for <see cref="User"/> lookups (Module 3 — the
/// first feature whose read patterns are shared across multiple services/
/// controllers, so a thin repository earns its keep here per the
/// project's "introduce one when it's warranted" convention). Business
/// logic (validation, mapping, deciding what changed) stays in
/// <c>Services/UserProfileService.cs</c> — this only fetches.
/// </summary>
public interface IUserRepository
{
    /// <summary>Fetches a user by id (excluding soft-deleted accounts), without roles loaded.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Fetches a user by id (excluding soft-deleted accounts) with roles eagerly loaded.</summary>
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
}
