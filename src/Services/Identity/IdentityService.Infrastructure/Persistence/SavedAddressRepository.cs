using IdentityService.Application.Abstractions;
using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Persistence;

public class SavedAddressRepository : ISavedAddressRepository
{
    private readonly IdentityDbContext _dbContext;

    public SavedAddressRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SavedAddress?> GetByIdAsync(Guid id)
    {
        return _dbContext.SavedAddresses.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<SavedAddress>> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.SavedAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(SavedAddress address)
    {
        _dbContext.SavedAddresses.Add(address);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(SavedAddress address)
    {
        _dbContext.SavedAddresses.Update(address);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<SavedAddress> addresses)
    {
        _dbContext.SavedAddresses.UpdateRange(addresses);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(SavedAddress address)
    {
        _dbContext.SavedAddresses.Remove(address);
        await _dbContext.SaveChangesAsync();
    }
}
