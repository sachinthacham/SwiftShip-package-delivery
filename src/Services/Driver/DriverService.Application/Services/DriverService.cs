using BuildingBlocks.Common;
using DriverService.Application.Abstractions;
using DriverService.Application.Common;
using DriverService.Application.DTOs;
using DriverService.Domain.Abstractions;
using DriverService.Domain.Entities;

namespace DriverService.Application.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _driverRepository;

    public DriverService(IDriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<DriverResponse> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken = default)
    {
        if (await _driverRepository.ExistsByUserIdAsync(request.UserId, cancellationToken))
        {
            throw new InvalidOperationException("This user already has a driver profile.");
        }

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            VehicleNumber = request.VehicleNumber,
            VehicleType = request.VehicleType,
            IsAvailable = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _driverRepository.AddAsync(driver, cancellationToken);
        return Map(driver);
    }

    public async Task<PaginatedList<DriverResponse>> GetPagedAsync(int pageNumber, int pageSize, bool? isAvailable, CancellationToken cancellationToken = default)
    {
        var (items, totalCount, resolvedPageNumber, resolvedPageSize) =
            await _driverRepository.GetPagedAsync(pageNumber, pageSize, isAvailable, cancellationToken);

        return new PaginatedList<DriverResponse>(items.Select(Map).ToList(), totalCount, resolvedPageNumber, resolvedPageSize);
    }

    public async Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        return driver is null ? null : Map(driver);
    }

    public async Task<DriverResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByUserIdAsync(userId, cancellationToken);
        return driver is null ? null : Map(driver);
    }

    public async Task<bool> SetAvailabilityAsync(Guid id, SetDriverAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        if (driver is null)
        {
            return false;
        }

        driver.IsAvailable = request.IsAvailable;
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        return true;
    }

    public async Task<bool> UpdateLocationAsync(Guid id, UpdateDriverLocationRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        if (driver is null)
        {
            return false;
        }

        driver.CurrentLatitude = request.Latitude;
        driver.CurrentLongitude = request.Longitude;
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NearbyDriverResponse>> GetAvailableNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
    {
        var candidates = await _driverRepository.GetAvailableWithLocationAsync(cancellationToken);

        return candidates
            .Select(d => new
            {
                Driver = d,
                DistanceKm = GeoDistanceCalculator.HaversineKm(latitude, longitude, d.CurrentLatitude!.Value, d.CurrentLongitude!.Value)
            })
            .Where(x => x.DistanceKm <= radiusKm)
            .OrderBy(x => x.DistanceKm)
            .Take(20)
            .Select(x => new NearbyDriverResponse(
                x.Driver.Id,
                x.Driver.UserId,
                x.Driver.Name,
                x.Driver.VehicleType.ToString(),
                x.Driver.CurrentLatitude!.Value,
                x.Driver.CurrentLongitude!.Value,
                x.DistanceKm))
            .ToList();
    }

    private static DriverResponse Map(Driver driver)
    {
        return new DriverResponse(
            driver.Id,
            driver.UserId,
            driver.Name,
            driver.VehicleNumber,
            driver.VehicleType.ToString(),
            driver.IsAvailable,
            driver.CurrentLatitude,
            driver.CurrentLongitude,
            driver.CreatedAtUtc);
    }
}
