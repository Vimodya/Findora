using Findora.API.Data;
using Findora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Findora.API.Repositories;

public class LostItemRepository : ILostItemRepository
{
    private readonly FindoraDbContext _db;

    public LostItemRepository(FindoraDbContext db)
    {
        _db = db;
    }

    public Task<LostItemReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.LostItemReports.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public Task<LostItemReport?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.LostItemReports
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public async Task<(List<LostItemReport> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.LostItemReports
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

    public void Add(LostItemReport report) => _db.LostItemReports.Add(report);
}
