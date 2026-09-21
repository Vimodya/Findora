using System.ComponentModel.DataAnnotations;
using Findora.API.Models;

namespace Findora.API.DTOs.Users;

/// <summary>
/// Body for <c>PATCH /api/v1/users/{id}/status</c> — Admin-only. Bound as
/// the actual <see cref="UserStatus"/> enum (serialized as its name, e.g.
/// <c>"Suspended"</c>, via the global <c>JsonStringEnumConverter</c>
/// registered in <c>Program.cs</c>) so an invalid/misspelled value is
/// rejected as a clean 400 during model binding, and an omitted value is
/// rejected by <c>[Required]</c> rather than silently defaulting to
/// <see cref="UserStatus.Active"/>.
/// </summary>
public class AdminUpdateUserStatusRequest
{
    [Required]
    public UserStatus? Status { get; set; }
}
