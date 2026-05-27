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

    public async Task<IReadOnlyList<Driver>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Drivers
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Drivers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        _dbContext.Drivers.Update(driver);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
