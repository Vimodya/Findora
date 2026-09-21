namespace Findora.API.Models;

/// <summary>
/// Found report status (Module 5). Deliberately minimal, mirroring
/// <see cref="LostItemStatus"/> — the full multi-stage lifecycle is
/// formalized by Module 15's <c>ReportLifecycleService</c>/state machine.
/// Values map cleanly onto Module 15's future state machine
/// (<see cref="Active"/> → <c>Reported</c>, <see cref="Returned"/> →
/// <c>Recovered</c>, <see cref="Cancelled"/> → <c>Archived</c>).
/// </summary>
public enum FoundItemStatus
{
    /// <summary>The item is still held by the finder/awaiting an owner; the report is visible/actionable.</summary>
    Active,

    /// <summary>The item was returned to its owner (manually marked for now — no automated claim/handover flow exists yet).</summary>
    Returned,

    /// <summary>The finder cancelled/withdrew the report.</summary>
    Cancelled
}
