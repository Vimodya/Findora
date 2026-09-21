namespace Findora.API.Models;

/// <summary>
/// A user's report of a lost item (Module 4). Designed to integrate
/// cleanly with later modules without needing schema surgery:
/// <see cref="Latitude"/>/<see cref="Longitude"/>/<see cref="LocationDescription"/>
/// feed Module 7's search/radius filtering; <see cref="Brand"/>/
/// <see cref="Color"/>/<see cref="CategoryId"/>/<see cref="IdentifyingCharacteristics"/>
/// feed Module 8/9's matching signals; <see cref="IdentifyingCharacteristics"/>/
/// <see cref="SerialNumber"/> double as the "hidden characteristics" Module
/// 11 uses for claim verification; <see cref="Status"/> maps cleanly onto
/// Module 15's formal lifecycle (see <see cref="LostItemStatus"/>).
///
/// Images are deliberately NOT stored here — no binary/URL column exists
/// yet. Module 6 introduces a separate <c>ReportImage</c> entity with its
/// own FK to this report, once file storage exists.
/// </summary>
public class LostItemReport : AuditableEntity
{
    /// <summary>The reporting/owning user. Always set server-side from the JWT, never from client input.</summary>
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid CategoryId { get; set; }

    public ItemCategory? Category { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>The date the item was lost (UTC, date-only in practice — time-of-day is captured separately by <see cref="ApproximateTimeLost"/>).</summary>
    public DateTime DateLost { get; set; }

    /// <summary>Optional approximate time of day the item was lost.</summary>
    public TimeOnly? ApproximateTimeLost { get; set; }

    /// <summary>Human-readable free-text location (e.g. "Near the library entrance, Main Campus").</summary>
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
    /// Free-text identifying details (scratches, engravings, contents,
    /// etc.) — later used by Module 11 as hidden-characteristic claim
    /// verification questions.
    /// </summary>
    public string? IdentifyingCharacteristics { get; set; }

    public string? SerialNumber { get; set; }

    public LostItemStatus Status { get; set; } = LostItemStatus.Active;

    public ContactPreference ContactPreference { get; set; } = ContactPreference.Platform;
}
