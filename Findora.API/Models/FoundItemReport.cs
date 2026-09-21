namespace Findora.API.Models;

/// <summary>
/// A user's report of a found item (Module 5) — the mirror of
/// <see cref="LostItemReport"/> for the other side of the lost/found
/// workflow. Designed to integrate cleanly with later modules the same way
/// <see cref="LostItemReport"/> does: <see cref="Latitude"/>/
/// <see cref="Longitude"/>/<see cref="LocationDescription"/> feed Module 7's
/// search/radius filtering; <see cref="Brand"/>/<see cref="Color"/>/
/// <see cref="CategoryId"/>/<see cref="IdentifyingCharacteristics"/> feed
/// Module 8/9's matching signals against lost reports; <see cref="Status"/>
/// maps cleanly onto Module 15's formal lifecycle (see
/// <see cref="FoundItemStatus"/>).
///
/// Images are deliberately NOT stored here — no binary/URL column exists
/// yet. Module 6 introduces a separate <c>ReportImage</c> entity with its
/// own FK to this report, once file storage exists.
/// </summary>
public class FoundItemReport : AuditableEntity
{
    /// <summary>The finder/reporting user. Always set server-side from the JWT, never from client input.</summary>
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid CategoryId { get; set; }

    public ItemCategory? Category { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>The date the item was found (UTC, date-only in practice — time-of-day is captured separately by <see cref="ApproximateTimeFound"/>).</summary>
    public DateTime DateFound { get; set; }

    /// <summary>Optional approximate time of day the item was found.</summary>
    public TimeOnly? ApproximateTimeFound { get; set; }

    /// <summary>Human-readable free-text location (e.g. "Left at the library front desk").</summary>
    public string? LocationDescription { get; set; }

    /// <summary>
    /// MVP location approach — plain lat/lng columns, app-level distance
    /// calculation in Module 7. No PostGIS/geometry column.
    /// </summary>
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Brand { get; set; }

    public string? Color { get; set; }

    /// <summary>
    /// Free-text identifying details noticed by the finder — later used by
    /// Module 11 to help verify a claimant's answers against a matching
    /// lost report's hidden characteristics.
    /// </summary>
    public string? IdentifyingCharacteristics { get; set; }

    public string? SerialNumber { get; set; }

    public FoundItemStatus Status { get; set; } = FoundItemStatus.Active;

    public ContactPreference ContactPreference { get; set; } = ContactPreference.Platform;
}
