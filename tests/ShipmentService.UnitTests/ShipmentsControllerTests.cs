using System.Security.Claims;
using BuildingBlocks.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ShipmentService.API.Controllers;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.DTOs;
using ShipmentService.Domain.Enums;

namespace ShipmentService.UnitTests;

// Unit tests for ShipmentsController
// The service is mocked so no DB, HTTP, or RabbitMQ run.
public class ShipmentsControllerTests
{
    private static readonly AddressDto Pickup = new("1 Pickup St", "City", "State", "00000", "Country");
    private static readonly AddressDto Delivery = new("1 Delivery St", "City", "State", "00000", "Country");

    private static void AttachUser(ShipmentsController controller, Guid userId, string role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role)],
            authenticationType: "Test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Create_ReturnsOk_WithBody_WhenServiceSucceeds()
    {
        var packageId = Guid.NewGuid();
        var request = new CreateShipmentRequest(packageId, Pickup, Delivery);
        var expected = new ShipmentResponse(
            Guid.NewGuid(),
            packageId,
            Guid.NewGuid(),
            null,
            "TRK-TEST",
            "Created",
            Pickup,
            Delivery,
            10.25m,
            "USD",
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
        var request = new CreateShipmentRequest(Guid.NewGuid(), Pickup, Delivery);
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
        var request = new CreateShipmentRequest(Guid.NewGuid(), Pickup, Delivery);
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
            Guid.NewGuid(),
            null,
            "TRK-1",
            "Created",
            Pickup,
            Delivery,
            10.25m,
            "USD",
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

    [Fact]
    public async Task GetPaged_ForcesCustomerIdFilter_WhenCallerIsCustomer()
    {
        var customerId = Guid.NewGuid();
        var expected = new PaginatedList<ShipmentResponse>([], 0, 1, 20);
        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.GetPagedAsync(1, 20, null, customerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ShipmentsController(mockService.Object);
        AttachUser(controller, customerId, "Customer");

        var result = await controller.GetPaged();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetPaged_ReturnsBadRequest_WhenCourierOmitsDriverId()
    {
        var controller = new ShipmentsController(new Mock<IShipmentService>().Object);
        AttachUser(controller, Guid.NewGuid(), "Courier");

        var result = await controller.GetPaged();

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetPaged_ReturnsOk_WhenCourierProvidesDriverId()
    {
        var driverId = Guid.NewGuid();
        var expected = new PaginatedList<ShipmentResponse>([], 0, 1, 20);
        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.GetPagedAsync(1, 20, null, null, driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ShipmentsController(mockService.Object);
        AttachUser(controller, Guid.NewGuid(), "Courier");

        var result = await controller.GetPaged(driverId: driverId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsConflict_WhenShipmentIsTerminal()
    {
        var id = Guid.NewGuid();
        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.UpdateStatusAsync(id, ShipmentStatus.InTransit, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Shipment is already Delivered and cannot be updated."));

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.UpdateStatus(id, new UpdateShipmentStatusRequest(ShipmentStatus.InTransit), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task AssignDriver_ReturnsNotFound_WhenShipmentMissing()
    {
        var id = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.AssignDriverAsync(id, driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShipmentResponse?)null);

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.AssignDriver(id, new AssignDriverRequest(driverId), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetInvoice_ReturnsForbid_WhenCustomerDoesNotOwnShipment()
    {
        var shipmentId = Guid.NewGuid();
        var response = new ShipmentResponse(
            shipmentId, Guid.NewGuid(), Guid.NewGuid(), null, "TRK-1", "Created", Pickup, Delivery, 10m, "USD", DateTime.UtcNow);
        var mockService = new Mock<IShipmentService>();
        mockService.Setup(s => s.GetByIdAsync(shipmentId)).ReturnsAsync(response);

        var controller = new ShipmentsController(mockService.Object);
        AttachUser(controller, Guid.NewGuid(), "Customer");

        var result = await controller.GetInvoice(shipmentId, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        mockService.Verify(s => s.GetInvoiceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetInvoice_ReturnsOk_WhenCustomerOwnsShipment()
    {
        var shipmentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new ShipmentResponse(
            shipmentId, Guid.NewGuid(), customerId, null, "TRK-1", "Created", Pickup, Delivery, 10m, "USD", DateTime.UtcNow);
        var invoice = new InvoiceResponse(Guid.NewGuid(), shipmentId, customerId, "INV-1", 10m, "USD", "Pending", DateTime.UtcNow, null);
        var mockService = new Mock<IShipmentService>();
        mockService.Setup(s => s.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        mockService.Setup(s => s.GetInvoiceAsync(shipmentId, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

        var controller = new ShipmentsController(mockService.Object);
        AttachUser(controller, customerId, "Customer");

        var result = await controller.GetInvoice(shipmentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(invoice, ok.Value);
    }

    [Fact]
    public async Task UpdatePaymentStatus_ReturnsNotFound_WhenInvoiceMissing()
    {
        var shipmentId = Guid.NewGuid();
        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.UpdatePaymentStatusAsync(shipmentId, PaymentStatus.Paid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceResponse?)null);

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.UpdatePaymentStatus(shipmentId, new UpdatePaymentStatusRequest(PaymentStatus.Paid), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UploadProofOfDelivery_ReturnsBadRequest_WhenFileMissing()
    {
        var controller = new ShipmentsController(new Mock<IShipmentService>().Object);

        var result = await controller.UploadProofOfDelivery(Guid.NewGuid(), Guid.NewGuid(), file: null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadProofOfDelivery_ReturnsBadRequest_WhenContentTypeIsNotAnImage()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var controller = new ShipmentsController(new Mock<IShipmentService>().Object);

        var result = await controller.UploadProofOfDelivery(Guid.NewGuid(), Guid.NewGuid(), fileMock.Object, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadProofOfDelivery_ReturnsOk_WhenAttachmentSucceeds()
    {
        var shipmentId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var response = new DeliveryAttemptResponse(attemptId, shipmentId, true, null, null, "/uploads/delivery-proofs/abc.jpg", DateTime.UtcNow, "Delivered");

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.FileName).Returns("proof.jpg");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));

        var mockService = new Mock<IShipmentService>();
        mockService
            .Setup(s => s.AttachProofOfDeliveryAsync(shipmentId, attemptId, It.IsAny<Stream>(), "proof.jpg", "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new ShipmentsController(mockService.Object);

        var result = await controller.UploadProofOfDelivery(shipmentId, attemptId, fileMock.Object, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }
}
