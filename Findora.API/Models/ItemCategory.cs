namespace Findora.API.Models;

/// <summary>
/// A lookup category for both lost and found items (Module 4, reused by
/// Module 5). Database-backed rather than a hard-coded enum so new
/// categories can be added later (e.g. by an Admin) without a code change
/// or migration.
/// </summary>
public class ItemCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Lets a category be retired from selection without breaking existing
    /// reports that already reference it (those keep their FK; only new
    /// reports are validated against active categories).
    /// </summary>
    public bool IsActive { get; set; } = true;
}
