using System.ComponentModel.DataAnnotations;
using Findora.API.Models;

namespace Findora.API.DTOs.FoundItems;

/// <summary>
/// Body for <c>PUT /api/v1/found-items/{id}</c>. Deliberately excludes
/// <c>Id</c>, <c>UserId</c>, <c>CreatedAt</c>, <c>UpdatedAt</c>,
/// <c>IsDeleted</c>, and <c>Status</c> — ownership, audit fields, and
/// status transitions are never client-editable through this endpoint (see
/// <c>FoundItemsController</c>/<c>FoundItemService</c>).
/// </summary>
public class UpdateFoundItemRequest : IValidatableObject
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
        if (DateFound.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Date found cannot be in the future.",
                new[] { nameof(DateFound) });
        }

        if (Latitude.HasValue != Longitude.HasValue)
        {
            yield return new ValidationResult(
                "Both latitude and longitude must be provided together.",
                new[] { nameof(Latitude), nameof(Longitude) });
        }
    }
}
