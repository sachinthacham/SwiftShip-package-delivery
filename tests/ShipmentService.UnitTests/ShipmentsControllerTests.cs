using Microsoft.AspNetCore.Mvc;
using Moq;
using ShipmentService.API.Controllers;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.DTOs;

namespace ShipmentService.UnitTests;

// Unit tests for ShipmentsController 
// The service is mocked so no DB, HTTP, or RabbitMQ run.
public class ShipmentsControllerTests
{
    [Fact]
    public async Task Create_ReturnsOk_WithBody_WhenServiceSucceeds()
    {
        var packageId = Guid.NewGuid();
        var request = new CreateShipmentRequest(packageId, "Pickup", "Delivery");
        var expected = new ShipmentResponse(
            Guid.NewGuid(),
            packageId,
            "TRK-TEST",
            "Created",
            "Pickup",
            "Delivery",
            DateTime.UtcNow);

        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.CreateAsync(request, "key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.Create(request, idempotencyKey: "key-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        mockService.Verify(
            s => s.CreateAsync(request, "key-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenPackageNotFound()
    {
        var request = new CreateShipmentRequest(Guid.NewGuid(), "A", "B");
        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.CreateAsync(request, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Package not found"));

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.Create(request, idempotencyKey: null, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenIdempotencyConflict()
    {
        var request = new CreateShipmentRequest(Guid.NewGuid(), "A", "B");
        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.CreateAsync(request, "dup-key", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("in-flight"));

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.Create(request, idempotencyKey: "dup-key", CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenShipmentExists()
    {
        var id = Guid.NewGuid();
        var response = new ShipmentResponse(
            id,
            Guid.NewGuid(),
            "TRK-1",
            "Created",
            "P",
            "D",
            DateTime.UtcNow);

        var mockService = new Mock<IShipmentService>();
        mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(response);

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.GetById(id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        var mockService = new Mock<IShipmentService>();
        mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((ShipmentResponse?)null);

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.GetById(id);

        Assert.IsType<NotFoundResult>(result);
    }
}
