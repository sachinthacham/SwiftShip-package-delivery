using IdentityService.Domain.Entities;

namespace IdentityService.Application.Abstractions;

public interface ISavedAddressRepository
{
    Task<SavedAddress?> GetByIdAsync(Guid id);
    Task<List<SavedAddress>> GetByUserIdAsync(Guid userId);
    Task AddAsync(SavedAddress address);
    Task UpdateAsync(SavedAddress address);
    Task UpdateRangeAsync(IEnumerable<SavedAddress> addresses);
    Task DeleteAsync(SavedAddress address);
}
