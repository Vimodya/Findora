namespace Findora.API.Models;

/// <summary>
/// Convention implemented by every persisted domain entity in Findora.
/// Provides the common identity and audit fields (<see cref="Id"/>,
/// <see cref="CreatedAt"/>, <see cref="UpdatedAt"/>, <see cref="IsDeleted"/>)
/// so that services, repositories, and EF Core configuration can rely on a
/// single consistent shape across all future modules (lost/found reports,
/// matches, claims, etc.).
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Primary key.</summary>
    Guid Id { get; set; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when the entity was last updated.</summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft-delete flag. Entities are never physically removed by default;
    /// queries should filter on <c>IsDeleted == false</c> (enforced via a
    /// global query filter once domain entities are introduced).
    /// </summary>
    bool IsDeleted { get; set; }
}
