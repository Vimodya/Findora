using Findora.API.Data;
using Findora.API.DTOs.LostItems;
using Findora.API.Mappings;
using Findora.API.Models;
using Findora.API.Repositories;

namespace Findora.API.Services;

public class LostItemService : ILostItemService
{
    private const int MaxPageSize = 50;

    private readonly ILostItemRepository _lostItemRepository;
    private readonly IItemCategoryRepository _categoryRepository;
    private readonly FindoraDbContext _db;
    private readonly ILogger<LostItemService> _logger;

    public LostItemService(
        ILostItemRepository lostItemRepository,
        IItemCategoryRepository categoryRepository,
        FindoraDbContext db,
        ILogger<LostItemService> logger)
    {
        _lostItemRepository = lostItemRepository;
        _categoryRepository = categoryRepository;
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<LostItemResponse>> CreateAsync(
        Guid userId,
        CreateLostItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.Validation, "The selected category does not exist.");
        }

        var report = new LostItemReport
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DateLost = DateTime.SpecifyKind(request.DateLost.Date, DateTimeKind.Utc),
            ApproximateTimeLost = request.ApproximateTimeLost,
            LocationDescription = NormalizeOptionalText(request.LocationDescription),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Brand = NormalizeOptionalText(request.Brand),
            Color = NormalizeOptionalText(request.Color),
            IdentifyingCharacteristics = NormalizeOptionalText(request.IdentifyingCharacteristics),
            SerialNumber = NormalizeOptionalText(request.SerialNumber),
            ContactPreference = request.ContactPreference,
            // Always server-assigned — never accepted from the client.
            Status = LostItemStatus.Active
        };

        _lostItemRepository.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        report.Category = category;

        _logger.LogInformation("User {UserId} created lost report {ReportId}.", userId, report.Id);

        return ServiceResult<LostItemResponse>.Success(report.ToResponse());
    }

    public async Task<ServiceResult<LostItemResponse>> GetByIdAsync(
        Guid userId,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var report = await _lostItemRepository.GetByIdWithCategoryAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.NotFound, "Lost report not found.");
        }

        if (report.UserId != userId)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.Forbidden, "You do not have access to this report.");
        }

        return ServiceResult<LostItemResponse>.Success(report.ToResponse());
    }

    public async Task<PaginatedLostItemsResponse> GetOwnReportsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        var (items, totalCount) = await _lostItemRepository.GetByUserAsync(userId, page, pageSize, cancellationToken);

        return new PaginatedLostItemsResponse
        {
            Items = items.Select(r => r.ToSummaryResponse()).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ServiceResult<LostItemResponse>> UpdateAsync(
        Guid userId,
        Guid reportId,
        UpdateLostItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await _lostItemRepository.GetByIdWithCategoryAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.NotFound, "Lost report not found.");
        }

        if (report.UserId != userId)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.Forbidden, "You do not have access to this report.");
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.Validation, "The selected category does not exist.");
        }

        // Only these fields are ever written — Id/UserId/CreatedAt/UpdatedAt/
        // IsDeleted/Status are not on UpdateLostItemRequest at all, so there
        // is nothing to accidentally copy over from client input.
        report.Title = request.Title.Trim();
        report.Description = request.Description.Trim();
        report.CategoryId = request.CategoryId;
        report.Category = category;
        report.DateLost = DateTime.SpecifyKind(request.DateLost.Date, DateTimeKind.Utc);
        report.ApproximateTimeLost = request.ApproximateTimeLost;
        report.LocationDescription = NormalizeOptionalText(request.LocationDescription);
        report.Latitude = request.Latitude;
        report.Longitude = request.Longitude;
        report.Brand = NormalizeOptionalText(request.Brand);
        report.Color = NormalizeOptionalText(request.Color);
        report.IdentifyingCharacteristics = NormalizeOptionalText(request.IdentifyingCharacteristics);
        report.SerialNumber = NormalizeOptionalText(request.SerialNumber);
        report.ContactPreference = request.ContactPreference;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated lost report {ReportId}.", userId, reportId);

        return ServiceResult<LostItemResponse>.Success(report.ToResponse());
    }

    public async Task<ServiceResult<LostItemResponse>> CancelAsync(
        Guid userId,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var report = await _lostItemRepository.GetByIdWithCategoryAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.NotFound, "Lost report not found.");
        }

        if (report.UserId != userId)
        {
            return ServiceResult<LostItemResponse>.Fail(ServiceErrorCode.Forbidden, "You do not have access to this report.");
        }

        report.Status = LostItemStatus.Cancelled;
        report.IsDeleted = true;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} cancelled lost report {ReportId}.", userId, reportId);

        return ServiceResult<LostItemResponse>.Success(report.ToResponse());
    }

    /// <summary>Treats whitespace-only input as "not provided" (null), mirroring <c>UserProfileService</c>'s convention.</summary>
    private static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
