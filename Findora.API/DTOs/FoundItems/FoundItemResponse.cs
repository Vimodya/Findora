using Findora.API.Models;

namespace Findora.API.DTOs.FoundItems;

/// <summary>
/// Full shape returned by create/retrieve-one/update on
/// <c>/api/v1/found-items</c>. Never derived from anything containing
/// <c>PasswordHash</c>, token data, or other <c>User</c> fields beyond the
/// finder's raw <see cref="UserId"/> itself.
/// </summary>
public class FoundItemResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime DateFound { get; set; }
    public TimeOnly? ApproximateTimeFound { get; set; }

    public string? LocationDescription { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Brand { get; set; }
    public string? Color { get; set; }
    public string? IdentifyingCharacteristics { get; set; }
    public string? SerialNumber { get; set; }

    public FoundItemStatus Status { get; set; }
    public ContactPreference ContactPreference { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
