using Findora.API.Models;

namespace Findora.API.DTOs.FoundItems;

/// <summary>Lighter-weight shape for list views (<c>GET /api/v1/found-items/my</c>) — omits the longer free-text fields.</summary>
public class FoundItemSummaryResponse
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime DateFound { get; set; }
    public string? LocationDescription { get; set; }

    public string? Brand { get; set; }
    public string? Color { get; set; }

    public FoundItemStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
