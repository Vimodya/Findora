using Findora.API.DTOs.FoundItems;

namespace Findora.API.Services;

/// <summary>Found-item report business logic (Module 5). Kept out of <c>FoundItemsController</c> so it stays thin, mirroring <see cref="ILostItemService"/>.</summary>
public interface IFoundItemService
{
    Task<ServiceResult<FoundItemResponse>> CreateAsync(Guid userId, CreateFoundItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>MVP visibility rule: only the finder may retrieve a single report by id (see <c>FoundItemsController</c> doc comment for the rationale — same approach as Module 4).</summary>
    Task<ServiceResult<FoundItemResponse>> GetByIdAsync(Guid userId, Guid reportId, CancellationToken cancellationToken = default);

    Task<PaginatedFoundItemsResponse> GetOwnReportsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ServiceResult<FoundItemResponse>> UpdateAsync(Guid userId, Guid reportId, UpdateFoundItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-cancels the report (finder only) — sets <see cref="Models.FoundItemStatus.Cancelled"/> and soft-deletes the row rather than physically removing it.</summary>
    Task<ServiceResult<FoundItemResponse>> CancelAsync(Guid userId, Guid reportId, CancellationToken cancellationToken = default);
}
