using BuildingBlocks.Common;
using PackageService.Application.DTOs;
using PackageService.Domain.Enums;

namespace PackageService.Application.Abstractions;

public interface IPackageService
{
    Task<PackageResponse> CreateAsync(CreatePackageRequest request, Guid senderId);
    Task<PackageResponse?> GetByIdAsync(Guid id);
    Task<PaginatedList<PackageResponse>> GetPagedAsync(int pageNumber, int pageSize, PackageStatus? status, Guid? senderId);
    Task<PackageResponse?> UpdateStatusAsync(Guid id, PackageStatus newStatus);
    Task<PackageResponse?> CancelAsync(Guid id);
}
