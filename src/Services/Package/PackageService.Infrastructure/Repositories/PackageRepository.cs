using Microsoft.EntityFrameworkCore;
using PackageService.Domain.Abstractions;
using PackageService.Domain.Entities;
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
}
