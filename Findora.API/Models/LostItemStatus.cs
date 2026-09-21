namespace Findora.API.Models;

/// <summary>
/// Lost report status (Module 4). Deliberately minimal — the full
/// multi-stage lifecycle (<c>UnderReview</c>, <c>Matched</c>,
/// <c>ClaimSubmitted</c>, etc.) is formalized by Module 15's
/// <c>ReportLifecycleService</c>/state machine. Until then, a lost report
/// only needs to distinguish "still looking" from "no longer active", and
/// this enum's values map cleanly onto whatever Module 15 introduces later
/// (<see cref="Active"/> → <c>Reported</c>, <see cref="Resolved"/> →
/// <c>Recovered</c>, <see cref="Cancelled"/> → <c>Archived</c>).
/// </summary>
public enum LostItemStatus
{
    /// <summary>The item is still lost; the report is visible/actionable.</summary>
    Active,

    /// <summary>The item was recovered (manually marked for now — no automated matching/claim flow exists yet).</summary>
    Resolved,

    /// <summary>The reporter cancelled/withdrew the report.</summary>
    Cancelled
}
