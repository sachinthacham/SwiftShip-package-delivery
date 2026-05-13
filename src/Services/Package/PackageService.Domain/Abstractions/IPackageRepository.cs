using PackageService.Domain.Entities;

namespace PackageService.Domain.Abstractions;

public interface IPackageRepository
{
    Task AddAsync(Package package);
    Task<Package?> GetByIdAsync(Guid id);
}
