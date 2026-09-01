using DriverService.Domain.Abstractions;
using DriverService.Domain.Entities;
using DriverService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Infrastructure.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly DriverDbContext _dbContext;

    public DriverRepository(DriverDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        _dbContext.Drivers.Add(driver);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Drivers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Drivers.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Drivers.AnyAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task UpdateAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        _dbContext.Drivers.Update(driver);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Driver> Items, int TotalCount, int PageNumber, int PageSize)> GetPagedAsync(
        int pageNumber, int pageSize, bool? isAvailable, CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _dbContext.Drivers.AsNoTracking().AsQueryable();

        if (isAvailable.HasValue)
            query = query.Where(x => x.IsAvailable == isAvailable.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<Driver>> GetAvailableWithLocationAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Drivers
            .AsNoTracking()
            .Where(x => x.IsAvailable && x.CurrentLatitude != null && x.CurrentLongitude != null)
            .ToListAsync(cancellationToken);
    }
}
