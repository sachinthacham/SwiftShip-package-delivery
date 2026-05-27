using DriverService.Application.Abstractions;
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
        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            VehicleNumber = request.VehicleNumber,
            IsAvailable = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _driverRepository.AddAsync(driver, cancellationToken);
        return Map(driver);
    }

    public async Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var drivers = await _driverRepository.GetAllAsync(cancellationToken);
        return drivers.Select(Map).ToList();
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

    private static DriverResponse Map(Driver driver)
    {
        return new DriverResponse(
            driver.Id,
            driver.Name,
            driver.VehicleNumber,
            driver.IsAvailable,
            driver.CreatedAtUtc);
    }
}
