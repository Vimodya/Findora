namespace Findora.API.Models;

/// <summary>
/// How the reporter would like to be contacted if someone responds to a
/// lost or found report (introduced in Module 4, reused as-is by Module
/// 5's found-item reporting). Purely a stored preference — no messaging
/// system exists yet (that's Module 12); actual contact details stay on
/// the user's profile and are never exposed through this field.
/// </summary>
public enum ContactPreference
{
    /// <summary>Default — via the platform's future messaging system (Module 12).</summary>
    Platform,

    Phone,

    Email
}
