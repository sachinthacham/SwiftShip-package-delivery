using PackageService.Domain.Entities;
using PackageService.Domain.Enums;

namespace PackageService.Domain.Abstractions;

public interface IPackageRepository
{
    Task AddAsync(Package package);
    Task<Package?> GetByIdAsync(Guid id);
    Task UpdateAsync(Package package);

    Task<(IReadOnlyList<Package> Items, int TotalCount, int PageNumber, int PageSize)> GetPagedAsync(
        int pageNumber, int pageSize, PackageStatus? status, Guid? senderId);
}
