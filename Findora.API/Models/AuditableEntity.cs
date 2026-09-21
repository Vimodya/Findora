namespace Findora.API.Models;

/// <summary>
/// Base class for all persisted domain entities in Findora. Implements the
/// shared <see cref="IAuditableEntity"/> convention so that every future
/// entity (lost reports, found reports, matches, claims, etc.) gets a
/// primary key and audit fields for free, without repeating them per model.
/// </summary>
public abstract class AuditableEntity : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
