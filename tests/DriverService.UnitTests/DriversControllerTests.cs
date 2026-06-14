using DriverService.API.Controllers;
using DriverService.Application.Abstractions;
using DriverService.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DriverService.UnitTests;

public class DriversControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreated_WithDriver()
    {
        var request = new CreateDriverRequest("Jane", "ABC-123");
        var response = new DriverResponse(Guid.NewGuid(), "Jane", "ABC-123", true, DateTime.UtcNow);
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = new DriversController(mock.Object);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(DriversController.GetAll), created.ActionName);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithDrivers()
    {
        var drivers = new List<DriverResponse>
        {
            new(Guid.NewGuid(), "A", "V1", false, DateTime.UtcNow)
        }.AsReadOnly();
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(drivers);

        var controller = new DriversController(mock.Object);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(drivers, ok.Value);
    }

    [Fact]
    public async Task SetAvailability_ReturnsNoContent_WhenUpdated()
    {
        var id = Guid.NewGuid();
        var request = new SetDriverAvailabilityRequest(true);
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.SetAvailabilityAsync(id, request, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = new DriversController(mock.Object);

        var result = await controller.SetAvailability(id, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetAvailability_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        var request = new SetDriverAvailabilityRequest(false);
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.SetAvailabilityAsync(id, request, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = new DriversController(mock.Object);

        var result = await controller.SetAvailability(id, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
