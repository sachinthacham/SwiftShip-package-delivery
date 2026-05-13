using PackageService.Application.DTOs;

namespace PackageService.Application.Abstractions;

public interface IPackageService
{
    Task<PackageResponse> CreateAsync(CreatePackageRequest request, Guid senderId);
    Task<PackageResponse?> GetByIdAsync(Guid id);
}
