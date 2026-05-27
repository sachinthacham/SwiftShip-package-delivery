using DriverService.Domain.Entities;

namespace DriverService.Domain.Abstractions;

public interface IDriverRepository
{
    Task AddAsync(Driver driver, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Driver>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Driver driver, CancellationToken cancellationToken = default);
}
