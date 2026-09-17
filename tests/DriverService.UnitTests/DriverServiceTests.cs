using DriverService.Application.DTOs;
using DriverService.Domain.Abstractions;
using DriverService.Domain.Entities;
using DriverService.Domain.Enums;
using Moq;
using ApplicationDriverService = DriverService.Application.Services.DriverService;

namespace DriverService.UnitTests;

public class DriverServiceTests
{
    private readonly Mock<IDriverRepository> _repo = new();

    private ApplicationDriverService CreateSut() => new(_repo.Object);

    [Fact]
    public async Task CreateAsync_AddsDriver_WhenUserHasNoExistingProfile()
    {
        var request = new CreateDriverRequest(Guid.NewGuid(), "Jane", "ABC-123", VehicleType.Car);
        _repo.Setup(r => r.ExistsByUserIdAsync(request.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();

        var result = await sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal("Jane", result.Name);
        Assert.True(result.IsAvailable);
        _repo.Verify(r => r.AddAsync(It.Is<Driver>(d => d.UserId == request.UserId && d.VehicleNumber == "ABC-123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenUserAlreadyHasDriverProfile()
    {
        var request = new CreateDriverRequest(Guid.NewGuid(), "Jane", "ABC-123", VehicleType.Car);
        _repo.Setup(r => r.ExistsByUserIdAsync(request.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(request, CancellationToken.None));
        _repo.Verify(r => r.AddAsync(It.IsAny<Driver>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetAvailabilityAsync_ReturnsFalse_WhenDriverMissing()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Driver?)null);

        var sut = CreateSut();

        var result = await sut.SetAvailabilityAsync(id, new SetDriverAvailabilityRequest(true), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task SetAvailabilityAsync_UpdatesFlag_WhenDriverExists()
    {
        var driver = new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "A", VehicleNumber = "V1", VehicleType = VehicleType.Van, IsAvailable = false };
        _repo.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

        var sut = CreateSut();

        var result = await sut.SetAvailabilityAsync(driver.Id, new SetDriverAvailabilityRequest(true), CancellationToken.None);

        Assert.True(result);
        Assert.True(driver.IsAvailable);
        _repo.Verify(r => r.UpdateAsync(driver, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLocationAsync_UpdatesCoordinates_WhenDriverExists()
    {
        var driver = new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "A", VehicleNumber = "V1", VehicleType = VehicleType.Bicycle };
        _repo.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

        var sut = CreateSut();

        var result = await sut.UpdateLocationAsync(driver.Id, new UpdateDriverLocationRequest(10.5, 20.5), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(10.5, driver.CurrentLatitude);
        Assert.Equal(20.5, driver.CurrentLongitude);
    }

    [Fact]
    public async Task GetAvailableNearbyAsync_ExcludesDriversOutsideRadius_AndOrdersByDistance()
    {
        var near = new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Near", VehicleNumber = "N1", VehicleType = VehicleType.Car, IsAvailable = true, CurrentLatitude = 40.001, CurrentLongitude = -75.0 };
        var far = new Driver { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Far", VehicleNumber = "F1", VehicleType = VehicleType.Car, IsAvailable = true, CurrentLatitude = 41.5, CurrentLongitude = -75.0 };
        _repo.Setup(r => r.GetAvailableWithLocationAsync(It.IsAny<CancellationToken>())).ReturnsAsync([far, near]);

        var sut = CreateSut();

        var result = await sut.GetAvailableNearbyAsync(40.0, -75.0, radiusKm: 5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Near", result[0].Name);
    }
}
