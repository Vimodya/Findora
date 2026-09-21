using Findora.API.Data;
using Findora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Findora.API.Repositories;

public class FoundItemRepository : IFoundItemRepository
{
    private readonly FindoraDbContext _db;

    public FoundItemRepository(FindoraDbContext db)
    {
        _db = db;
    }

    public Task<FoundItemReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.FoundItemReports.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public Task<FoundItemReport?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.FoundItemReports
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public async Task<(List<FoundItemReport> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.FoundItemReports
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(FoundItemReport report) => _db.FoundItemReports.Add(report);
}
