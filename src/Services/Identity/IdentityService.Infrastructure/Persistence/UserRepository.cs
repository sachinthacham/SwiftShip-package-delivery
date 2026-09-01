using BuildingBlocks.Common;
using IdentityService.Application.Abstractions;
using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        return _dbContext.Users.AnyAsync(u => u.Email == email);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public Task<PaginatedList<User>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var query = _dbContext.Users.OrderBy(u => u.CreatedAt).AsQueryable();
        return PaginatedList<User>.CreateAsync(query, pageNumber, pageSize);
    }
}
