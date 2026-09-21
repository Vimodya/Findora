using Findora.API.DTOs.LostItems;

namespace Findora.API.Services;

/// <summary>Lost-item report business logic (Module 4). Kept out of <c>LostItemsController</c> so it stays thin.</summary>
public interface ILostItemService
{
    Task<ServiceResult<LostItemResponse>> CreateAsync(Guid userId, CreateLostItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>MVP visibility rule: only the owner may retrieve a single report by id (see <c>LostItemsController</c> doc comment for the rationale).</summary>
    Task<ServiceResult<LostItemResponse>> GetByIdAsync(Guid userId, Guid reportId, CancellationToken cancellationToken = default);

    Task<PaginatedLostItemsResponse> GetOwnReportsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ServiceResult<LostItemResponse>> UpdateAsync(Guid userId, Guid reportId, UpdateLostItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-cancels the report (owner only) — sets <see cref="Models.LostItemStatus.Cancelled"/> and soft-deletes the row rather than physically removing it.</summary>
    Task<ServiceResult<LostItemResponse>> CancelAsync(Guid userId, Guid reportId, CancellationToken cancellationToken = default);
}
