using Findora.API.DTOs.ItemCategories;
using Findora.API.DTOs.LostItems;
using Findora.API.Models;

namespace Findora.API.Mappings;

/// <summary>Hand-written entity-to-DTO mapping for lost-item reports and item categories (Module 4) — mirrors <c>UserMappingExtensions</c>' approach.</summary>
public static class LostItemMappingExtensions
{
    /// <summary>
    /// Maps to the full response shape. Requires <see cref="LostItemReport.Category"/>
    /// to be loaded (see <c>ILostItemRepository.GetByIdWithCategoryAsync</c>).
    /// </summary>
    public static LostItemResponse ToResponse(this LostItemReport report)
    {
        return new LostItemResponse
        {
            Id = report.Id,
            UserId = report.UserId,
            CategoryId = report.CategoryId,
            CategoryName = report.Category?.Name ?? string.Empty,
            Title = report.Title,
            Description = report.Description,
            DateLost = report.DateLost,
            ApproximateTimeLost = report.ApproximateTimeLost,
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

    /// <summary>Maps to the lighter-weight list shape. Also requires <see cref="LostItemReport.Category"/> to be loaded.</summary>
    public static LostItemSummaryResponse ToSummaryResponse(this LostItemReport report)
    {
        return new LostItemSummaryResponse
        {
            Id = report.Id,
            CategoryId = report.CategoryId,
            CategoryName = report.Category?.Name ?? string.Empty,
            Title = report.Title,
            DateLost = report.DateLost,
            LocationDescription = report.LocationDescription,
            Brand = report.Brand,
            Color = report.Color,
            Status = report.Status,
            CreatedAt = report.CreatedAt
        };
    }

    public static ItemCategoryResponse ToResponse(this ItemCategory category)
    {
        return new ItemCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }
}
