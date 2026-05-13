using PackageService.Application.Abstractions;
using PackageService.Application.DTOs;
using PackageService.Domain.Abstractions;
using PackageService.Domain.Entities;

namespace PackageService.Application.Services;

public class PackageService : IPackageService
{
    private readonly IPackageRepository _packages;

    public PackageService(IPackageRepository packages)
    {
        _packages = packages;
    }

    public async Task<PackageResponse> CreateAsync(CreatePackageRequest request, Guid senderId)
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            ReceiverAddress = request.ReceiverAddress,
            Weight = request.Weight,
            Length = request.Length,
            Width = request.Width,
            Height = request.Height,
            Status = "Created",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _packages.AddAsync(package);

        return MapToResponse(package);
    }

    public async Task<PackageResponse?> GetByIdAsync(Guid id)
    {
        var package = await _packages.GetByIdAsync(id);
        if (package == null) return null;

        return MapToResponse(package);
    }

    private static PackageResponse MapToResponse(Package package)
    {
        return new PackageResponse(
            package.Id,
            package.SenderId,
            package.ReceiverName,
            package.ReceiverPhone,
            package.ReceiverAddress,
            package.Weight,
            package.Length,
            package.Width,
            package.Height,
            package.Status,
            package.CreatedAt
        );
    }
}