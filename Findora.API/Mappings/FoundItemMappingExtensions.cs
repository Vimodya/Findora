using Findora.API.DTOs.FoundItems;
using Findora.API.Models;

namespace Findora.API.Mappings;

/// <summary>
/// Hand-written entity-to-DTO mapping for found-item reports (Module 5) —
/// mirrors <c>LostItemMappingExtensions</c>' approach. The shared
/// <c>ItemCategory.ToResponse()</c> mapping already lives in
/// <c>LostItemMappingExtensions</c> and is reused as-is here — no need to
/// duplicate it.
/// </summary>
public static class FoundItemMappingExtensions
{
    /// <summary>
    /// Maps to the full response shape. Requires <see cref="FoundItemReport.Category"/>
    /// to be loaded (see <c>IFoundItemRepository.GetByIdWithCategoryAsync</c>).
    /// </summary>
    public static FoundItemResponse ToResponse(this FoundItemReport report)
    {
        return new FoundItemResponse
        {
            Id = report.Id,
            UserId = report.UserId,
            CategoryId = report.CategoryId,
            CategoryName = report.Category?.Name ?? string.Empty,
            Title = report.Title,
            Description = report.Description,
            DateFound = report.DateFound,
            ApproximateTimeFound = report.ApproximateTimeFound,
            LocationDescription = report.LocationDescription,
            Latitude = report.Latitude,
            Longitude = report.Longitude,
            Brand = report.Brand,
            Color = report.Color,
            IdentifyingCharacteristics = report.IdentifyingCharacteristics,
            SerialNumber = report.SerialNumber,
            Status = report.Status,
            ContactPreference = report.ContactPreference,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    /// <summary>Maps to the lighter-weight list shape. Also requires <see cref="FoundItemReport.Category"/> to be loaded.</summary>
    public static FoundItemSummaryResponse ToSummaryResponse(this FoundItemReport report)
    {
        return new FoundItemSummaryResponse
        {
            Id = report.Id,
            CategoryId = report.CategoryId,
            CategoryName = report.Category?.Name ?? string.Empty,
            Title = report.Title,
            DateFound = report.DateFound,
            LocationDescription = report.LocationDescription,
            Brand = report.Brand,
            Color = report.Color,
            Status = report.Status,
            CreatedAt = report.CreatedAt
        };
    }
}
