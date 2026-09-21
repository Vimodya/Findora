using System.ComponentModel.DataAnnotations;
using Findora.API.Models;

namespace Findora.API.DTOs.LostItems;

/// <summary>
/// Body for <c>POST /api/v1/lost-items</c>. Deliberately has no
/// <c>UserId</c>, <c>Id</c>, <c>Status</c>, or audit-field property at
/// all — the reporter is always taken from the authenticated caller's JWT
/// (see <c>LostItemsController</c>), and the initial status is always
/// server-assigned (<see cref="Models.LostItemStatus.Active"/>).
/// </summary>
public class CreateLostItemRequest : IValidatableObject
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
    public DateTime DateLost { get; set; }

    public TimeOnly? ApproximateTimeLost { get; set; }

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
        // A future date can't be when the item was "lost" — reject rather
        // than silently accepting nonsensical data.
        if (DateLost.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Date lost cannot be in the future.",
                new[] { nameof(DateLost) });
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
