using BuildingBlocks.Common;
using PackageService.Application.Abstractions;
using PackageService.Application.DTOs;
using PackageService.Domain.Abstractions;
using PackageService.Domain.Entities;
using PackageService.Domain.Enums;
using PackageService.Domain.ValueObjects;

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
            ReceiverAddress = new Address
            {
                Street = request.ReceiverAddress.Street,
                City = request.ReceiverAddress.City,
                State = request.ReceiverAddress.State,
                PostalCode = request.ReceiverAddress.PostalCode,
                Country = request.ReceiverAddress.Country,
                Latitude = request.ReceiverAddress.Latitude,
                Longitude = request.ReceiverAddress.Longitude
            },
            Weight = request.Weight,
            Length = request.Length,
            Width = request.Width,
            Height = request.Height,
            DeclaredValue = request.DeclaredValue,
            DeliveryType = request.DeliveryType,
            Status = PackageStatus.Created,
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

    public async Task<PaginatedList<PackageResponse>> GetPagedAsync(int pageNumber, int pageSize, PackageStatus? status, Guid? senderId)
    {
        var (items, totalCount, resolvedPageNumber, resolvedPageSize) =
            await _packages.GetPagedAsync(pageNumber, pageSize, status, senderId);

        return new PaginatedList<PackageResponse>(
            items.Select(MapToResponse).ToList(), totalCount, resolvedPageNumber, resolvedPageSize);
    }

    public async Task<PackageResponse?> UpdateStatusAsync(Guid id, PackageStatus newStatus)
    {
        var package = await _packages.GetByIdAsync(id);
        if (package is null) return null;

        if (package.Status is PackageStatus.Delivered or PackageStatus.Cancelled)
            throw new InvalidOperationException($"Package is already {package.Status} and cannot be updated.");

        package.Status = newStatus;
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.UpdateAsync(package);

        return MapToResponse(package);
    }

    public Task<PackageResponse?> CancelAsync(Guid id) => UpdateStatusAsync(id, PackageStatus.Cancelled);

    private static PackageResponse MapToResponse(Package package)
    {
        return new PackageResponse(
            package.Id,
            package.SenderId,
            package.ReceiverName,
            package.ReceiverPhone,
            new AddressDto(
                package.ReceiverAddress.Street,
                package.ReceiverAddress.City,
                package.ReceiverAddress.State,
                package.ReceiverAddress.PostalCode,
                package.ReceiverAddress.Country,
                package.ReceiverAddress.Latitude,
                package.ReceiverAddress.Longitude
            ),
            package.Weight,
            package.Length,
            package.Width,
            package.Height,
            package.DeclaredValue,
            package.DeliveryType.ToString(),
            package.Status.ToString(),
            package.CreatedAt
        );
    }
}