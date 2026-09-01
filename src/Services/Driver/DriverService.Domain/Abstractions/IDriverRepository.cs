using DriverService.Domain.Entities;

namespace DriverService.Domain.Abstractions;

public interface IDriverRepository
{
    Task AddAsync(Driver driver, CancellationToken cancellationToken = default);
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Driver driver, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Driver> Items, int TotalCount, int PageNumber, int PageSize)> GetPagedAsync(
        int pageNumber, int pageSize, bool? isAvailable, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Driver>> GetAvailableWithLocationAsync(CancellationToken cancellationToken = default);
}
