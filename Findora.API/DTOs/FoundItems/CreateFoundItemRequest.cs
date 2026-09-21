using System.ComponentModel.DataAnnotations;
using Findora.API.Models;

namespace Findora.API.DTOs.FoundItems;

/// <summary>
/// Body for <c>POST /api/v1/found-items</c>. Mirrors
/// <c>CreateLostItemRequest</c>'s shape — deliberately has no
/// <c>UserId</c>, <c>Id</c>, <c>Status</c>, or audit-field property at
/// all. The finder is always taken from the authenticated caller's JWT
/// (see <c>FoundItemsController</c>), and the initial status is always
/// server-assigned (<see cref="Models.FoundItemStatus.Active"/>).
/// </summary>
public class CreateFoundItemRequest : IValidatableObject
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public DateTime DateFound { get; set; }

    public TimeOnly? ApproximateTimeFound { get; set; }

    [MaxLength(500)]
    public string? LocationDescription { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(2000)]
    public string? IdentifyingCharacteristics { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    public ContactPreference ContactPreference { get; set; } = ContactPreference.Platform;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // A future date can't be when the item was "found" — reject rather
        // than silently accepting nonsensical data.
        if (DateFound.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Date found cannot be in the future.",
                new[] { nameof(DateFound) });
        }

        // A partial coordinate pair (only one of the two) is not a usable
        // location — either provide both or neither.
        if (Latitude.HasValue != Longitude.HasValue)
        {
            yield return new ValidationResult(
                "Both latitude and longitude must be provided together.",
                new[] { nameof(Latitude), nameof(Longitude) });
        }
    }
}
