using Findora.API.Models;

namespace Findora.API.DTOs.LostItems;

/// <summary>Lighter-weight shape for list views (<c>GET /api/v1/lost-items/my</c>) — omits the longer free-text fields.</summary>
public class LostItemSummaryResponse
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime DateLost { get; set; }
    public string? LocationDescription { get; set; }

    public string? Brand { get; set; }
    public string? Color { get; set; }

    public LostItemStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
