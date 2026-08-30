using Microsoft.EntityFrameworkCore;
using PackageService.Domain.Abstractions;
using PackageService.Domain.Entities;
using PackageService.Domain.Enums;
using PackageService.Infrastructure.Persistence;

namespace PackageService.Infrastructure.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly PackageDbContext _dbContext;

    public PackageRepository(PackageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Package package)
    {
        _dbContext.Packages.Add(package);
        await _dbContext.SaveChangesAsync();
    }

    public Task<Package?> GetByIdAsync(Guid id)
    {
        return _dbContext.Packages.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateAsync(Package package)
    {
        _dbContext.Packages.Update(package);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<Package> Items, int TotalCount, int PageNumber, int PageSize)> GetPagedAsync(
        int pageNumber, int pageSize, PackageStatus? status, Guid? senderId)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _dbContext.Packages.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (senderId.HasValue)
            query = query.Where(p => p.SenderId == senderId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount, pageNumber, pageSize);
    }
}
