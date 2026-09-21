using Findora.API.Data;
using Findora.API.DTOs.FoundItems;
using Findora.API.Mappings;
using Findora.API.Models;
using Findora.API.Repositories;

namespace Findora.API.Services;

/// <summary>
/// Mirrors <see cref="LostItemService"/> field-for-field, with
/// <c>DateFound</c>/<c>ApproximateTimeFound</c> in place of
/// <c>DateLost</c>/<c>ApproximateTimeLost</c> and <see cref="FoundItemStatus"/>
/// in place of <see cref="LostItemStatus"/>. Reuses the same
/// <see cref="IItemCategoryRepository"/> Module 4 introduced — no separate
/// found-item category lookup.
/// </summary>
public class FoundItemService : IFoundItemService
{
    private const int MaxPageSize = 50;

    private readonly IFoundItemRepository _foundItemRepository;
    private readonly IItemCategoryRepository _categoryRepository;
    private readonly FindoraDbContext _db;
    private readonly ILogger<FoundItemService> _logger;

    public FoundItemService(
        IFoundItemRepository foundItemRepository,
        IItemCategoryRepository categoryRepository,
        FindoraDbContext db,
        ILogger<FoundItemService> logger)
    {
        _foundItemRepository = foundItemRepository;
        _categoryRepository = categoryRepository;
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<FoundItemResponse>> CreateAsync(
        Guid userId,
        CreateFoundItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.Validation, "The selected category does not exist.");
        }

        var report = new FoundItemReport
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DateFound = DateTime.SpecifyKind(request.DateFound.Date, DateTimeKind.Utc),
            ApproximateTimeFound = request.ApproximateTimeFound,
            LocationDescription = NormalizeOptionalText(request.LocationDescription),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Brand = NormalizeOptionalText(request.Brand),
            Color = NormalizeOptionalText(request.Color),
            IdentifyingCharacteristics = NormalizeOptionalText(request.IdentifyingCharacteristics),
            SerialNumber = NormalizeOptionalText(request.SerialNumber),
            ContactPreference = request.ContactPreference,
            // Always server-assigned — never accepted from the client.
            Status = FoundItemStatus.Active
        };

        _foundItemRepository.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        report.Category = category;

        _logger.LogInformation("User {UserId} created found report {ReportId}.", userId, report.Id);

        return ServiceResult<FoundItemResponse>.Success(report.ToResponse());
    }

    public async Task<ServiceResult<FoundItemResponse>> GetByIdAsync(
        Guid userId,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var report = await _foundItemRepository.GetByIdWithCategoryAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.NotFound, "Found report not found.");
        }

        if (report.UserId != userId)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.Forbidden, "You do not have access to this report.");
        }

        return ServiceResult<FoundItemResponse>.Success(report.ToResponse());
    }

    public async Task<PaginatedFoundItemsResponse> GetOwnReportsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        var (items, totalCount) = await _foundItemRepository.GetByUserAsync(userId, page, pageSize, cancellationToken);

        return new PaginatedFoundItemsResponse
        {
            Items = items.Select(r => r.ToSummaryResponse()).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ServiceResult<FoundItemResponse>> UpdateAsync(
        Guid userId,
        Guid reportId,
        UpdateFoundItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await _foundItemRepository.GetByIdWithCategoryAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.NotFound, "Found report not found.");
        }

        if (report.UserId != userId)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.Forbidden, "You do not have access to this report.");
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.Validation, "The selected category does not exist.");
        }

        // Only these fields are ever written — Id/UserId/CreatedAt/UpdatedAt/
        // IsDeleted/Status are not on UpdateFoundItemRequest at all, so there
        // is nothing to accidentally copy over from client input.
        report.Title = request.Title.Trim();
        report.Description = request.Description.Trim();
        report.CategoryId = request.CategoryId;
        report.Category = category;
        report.DateFound = DateTime.SpecifyKind(request.DateFound.Date, DateTimeKind.Utc);
        report.ApproximateTimeFound = request.ApproximateTimeFound;
        report.LocationDescription = NormalizeOptionalText(request.LocationDescription);
        report.Latitude = request.Latitude;
        report.Longitude = request.Longitude;
        report.Brand = NormalizeOptionalText(request.Brand);
        report.Color = NormalizeOptionalText(request.Color);
        report.IdentifyingCharacteristics = NormalizeOptionalText(request.IdentifyingCharacteristics);
        report.SerialNumber = NormalizeOptionalText(request.SerialNumber);
        report.ContactPreference = request.ContactPreference;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated found report {ReportId}.", userId, reportId);

        return ServiceResult<FoundItemResponse>.Success(report.ToResponse());
    }

    public async Task<ServiceResult<FoundItemResponse>> CancelAsync(
        Guid userId,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var report = await _foundItemRepository.GetByIdWithCategoryAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.NotFound, "Found report not found.");
        }

        if (report.UserId != userId)
        {
            return ServiceResult<FoundItemResponse>.Fail(ServiceErrorCode.Forbidden, "You do not have access to this report.");
        }

        report.Status = FoundItemStatus.Cancelled;
        report.IsDeleted = true;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} cancelled found report {ReportId}.", userId, reportId);

        return ServiceResult<FoundItemResponse>.Success(report.ToResponse());
    }

    /// <summary>Treats whitespace-only input as "not provided" (null), mirroring <c>LostItemService</c>'s convention.</summary>
    private static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
