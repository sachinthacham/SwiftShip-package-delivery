using System.Security.Claims;
using BuildingBlocks.Common;
using DriverService.API.Controllers;
using DriverService.Application.Abstractions;
using DriverService.Application.DTOs;
using DriverService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DriverService.UnitTests;

public class DriversControllerTests
{
    private static void AttachUser(DriversController controller, Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            authenticationType: "Test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithDriver()
    {
        var userId = Guid.NewGuid();
        var request = new CreateDriverRequest(userId, "Jane", "ABC-123", VehicleType.Car);
        var response = new DriverResponse(Guid.NewGuid(), userId, "Jane", "ABC-123", "Car", true, null, null, DateTime.UtcNow);
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = new DriversController(mock.Object);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(DriversController.GetById), created.ActionName);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public async Task GetPaged_ReturnsOk_WithDrivers()
    {
        var page = new PaginatedList<DriverResponse>(
            [new DriverResponse(Guid.NewGuid(), Guid.NewGuid(), "A", "V1", "Car", false, null, null, DateTime.UtcNow)],
            totalCount: 1, pageNumber: 1, pageSize: 20);
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>())).ReturnsAsync(page);

        var controller = new DriversController(mock.Object);

        var result = await controller.GetPaged(cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(page, ok.Value);
    }

    [Fact]
    public async Task GetMe_ReturnsNotFound_WhenNoDriverProfileLinked()
    {
        var userId = Guid.NewGuid();
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((DriverResponse?)null);

        var controller = new DriversController(mock.Object);
        AttachUser(controller, userId);

        var result = await controller.GetMe(CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMe_ReturnsOk_WhenDriverProfileExists()
    {
        var userId = Guid.NewGuid();
        var response = new DriverResponse(Guid.NewGuid(), userId, "A", "V1", "Car", true, null, null, DateTime.UtcNow);
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = new DriversController(mock.Object);
        AttachUser(controller, userId);

        var result = await controller.GetMe(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
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

    [Fact]
    public async Task UpdateMyLocation_ReturnsNotFound_WhenNoDriverProfileLinked()
    {
        var userId = Guid.NewGuid();
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((DriverResponse?)null);

        var controller = new DriversController(mock.Object);
        AttachUser(controller, userId);

        var result = await controller.UpdateMyLocation(new UpdateDriverLocationRequest(1, 2), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateMyLocation_ReturnsNoContent_WhenDriverProfileExists()
    {
        var userId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var response = new DriverResponse(driverId, userId, "A", "V1", "Car", true, null, null, DateTime.UtcNow);
        var request = new UpdateDriverLocationRequest(1, 2);
        var mock = new Mock<IDriverService>();
        mock.Setup(d => d.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        mock.Setup(d => d.UpdateLocationAsync(driverId, request, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var controller = new DriversController(mock.Object);
        AttachUser(controller, userId);

        var result = await controller.UpdateMyLocation(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
