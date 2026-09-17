using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.DTOs;
using ShipmentService.Application.Pricing;
using ShipmentService.Domain.Abstractions;
using ShipmentService.Domain.Entities;
using ShipmentService.Domain.Enums;
using ShipmentService.Domain.ValueObjects;
using AppShipmentService = ShipmentService.Application.Services.ShipmentService;

namespace ShipmentService.UnitTests;

// Unit tests for the Application-layer ShipmentService, with every dependency mocked
// (repository, package validation, event publisher, driver availability, payment gateway,
// file storage, validator) so nothing here hits a DB, RabbitMQ, gRPC, or Stripe.
public class ShipmentApplicationServiceTests
{
    private static readonly AddressDto PickupDto = new("1 Pickup St", "City", "State", "00000", "Country", 10, 20);
    private static readonly AddressDto DeliveryDto = new("1 Delivery St", "City", "State", "00000", "Country", 11, 21);

    private readonly Mock<IShipmentRepository> _repo = new();
    private readonly Mock<IPackageValidationClient> _packageClient = new();
    private readonly Mock<IShipmentEventPublisher> _publisher = new();
    private readonly Mock<IShipmentPricingCalculator> _pricing = new();
    private readonly Mock<BuildingBlocks.FileStorage.IFileStorageService> _fileStorage = new();
    private readonly Mock<IDriverAvailabilityClient> _driverClient = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IValidator<CreateShipmentRequest>> _validator = new();
    private readonly Mock<ILogger<AppShipmentService>> _logger = new();

    private AppShipmentService CreateSut()
    {
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        return new AppShipmentService(
            _repo.Object,
            _packageClient.Object,
            _publisher.Object,
            _pricing.Object,
            _fileStorage.Object,
            _driverClient.Object,
            _paymentGateway.Object,
            _validator.Object,
            _logger.Object);
    }

    private static Shipment MakeShipment(Guid? id = null, ShipmentStatus status = ShipmentStatus.Created, Guid? customerId = null)
    {
        return new Shipment
        {
            Id = id ?? Guid.NewGuid(),
            PackageId = Guid.NewGuid(),
            CustomerId = customerId ?? Guid.NewGuid(),
            TrackingNumber = "TRK-TEST-1",
            Status = status,
            DeliveryType = "Standard",
            PickupAddress = new Address { Street = "1 Pickup St", City = "City", State = "State", PostalCode = "00000", Country = "Country", Latitude = 10, Longitude = 20 },
            DeliveryAddress = new Address { Street = "1 Delivery St", City = "City", State = "State", PostalCode = "00000", Country = "Country", Latitude = 11, Longitude = 21 },
            Cost = 10.25m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // ---- Create / idempotency ----

    [Fact]
    public async Task CreateAsync_WithIdempotencyKey_ReturnsExistingShipment_WithoutCreatingDuplicate()
    {
        var existing = MakeShipment();
        _repo.Setup(r => r.GetShipmentIdByIdempotencyKeyAsync("key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.Id);
        _repo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = CreateSut();
        var request = new CreateShipmentRequest(existing.PackageId, PickupDto, DeliveryDto);

        var result1 = await sut.CreateAsync(request, "key-1");
        var result2 = await sut.CreateAsync(request, "key-1");

        Assert.Equal(existing.Id, result1.Id);
        Assert.Equal(existing.Id, result2.Id);
        _repo.Verify(r => r.AddAsync(It.IsAny<Shipment>(), It.IsAny<ShipmentStatusHistory>(), It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        _packageClient.Verify(p => p.GetPackageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NewIdempotencyKey_CreatesShipment_AndPublishesEvent()
    {
        var packageId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        _repo.Setup(r => r.GetShipmentIdByIdempotencyKeyAsync("key-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        _repo.Setup(r => r.TryReserveIdempotencyKeyAsync("key-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _packageClient.Setup(p => p.GetPackageAsync(packageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PackageValidationResult(5m, "Express", senderId));
        _pricing.Setup(p => p.Calculate(5m, "Express")).Returns(20m);

        var sut = CreateSut();
        var request = new CreateShipmentRequest(packageId, PickupDto, DeliveryDto);

        var result = await sut.CreateAsync(request, "key-2");

        Assert.Equal(senderId, result.CustomerId);
        Assert.Equal(20m, result.Cost);
        _repo.Verify(r => r.AddAsync(It.IsAny<Shipment>(), It.IsAny<ShipmentStatusHistory>(), It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SetIdempotencyResultAsync("key-2", It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(p => p.PublishShipmentCreatedAsync(It.IsAny<BuildingBlocks.IntegrationEvents.ShipmentCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ConcurrentSameKey_WhenReservationFails_ThrowsIfNoResultYet()
    {
        _repo.Setup(r => r.GetShipmentIdByIdempotencyKeyAsync("key-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        _repo.Setup(r => r.TryReserveIdempotencyKeyAsync("key-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var request = new CreateShipmentRequest(Guid.NewGuid(), PickupDto, DeliveryDto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(request, "key-3"));
    }

    [Fact]
    public async Task CreateAsync_ThrowsKeyNotFound_WhenPackageDoesNotExist()
    {
        _packageClient.Setup(p => p.GetPackageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PackageValidationResult?)null);

        var sut = CreateSut();
        var request = new CreateShipmentRequest(Guid.NewGuid(), PickupDto, DeliveryDto);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.CreateAsync(request));
        _publisher.Verify(p => p.PublishShipmentCreatedAsync(It.IsAny<BuildingBlocks.IntegrationEvents.ShipmentCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Status transitions ----

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_UpdatesAndPublishes()
    {
        var shipment = MakeShipment(status: ShipmentStatus.PickedUp);
        var updated = MakeShipment(id: shipment.Id, status: ShipmentStatus.InTransit, customerId: shipment.CustomerId);
        _repo.Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);
        _repo.Setup(r => r.UpdateStatusAsync(shipment.Id, ShipmentStatus.InTransit, It.IsAny<ShipmentStatusHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var sut = CreateSut();
        var result = await sut.UpdateStatusAsync(shipment.Id, ShipmentStatus.InTransit);

        Assert.NotNull(result);
        Assert.Equal("InTransit", result!.Status);
        _publisher.Verify(p => p.PublishShipmentStatusChangedAsync(It.IsAny<BuildingBlocks.IntegrationEvents.ShipmentStatusChangedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task UpdateStatusAsync_ThrowsInvalidOperation_WhenShipmentIsTerminal(ShipmentStatus terminalStatus)
    {
        var shipment = MakeShipment(status: terminalStatus);
        _repo.Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateStatusAsync(shipment.Id, ShipmentStatus.InTransit));
        _repo.Verify(r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<ShipmentStatus>(), It.IsAny<ShipmentStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Assign driver ----

    [Fact]
    public async Task AssignDriverAsync_ReturnsMappedShipment_WhenAssignmentSucceeds()
    {
        var driverId = Guid.NewGuid();
        var shipment = MakeShipment();
        shipment.DriverId = driverId;
        _repo.Setup(r => r.AssignDriverAsync(shipment.Id, driverId, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

        var sut = CreateSut();
        var result = await sut.AssignDriverAsync(shipment.Id, driverId);

        Assert.NotNull(result);
        Assert.Equal(driverId, result!.DriverId);
    }

    [Fact]
    public async Task AutoAssignDriverAsync_PicksNearestDriver_AndAssigns()
    {
        var shipment = MakeShipment();
        var nearest = new NearbyDriverResult(Guid.NewGuid(), "Nearest Driver", "Motorcycle", 1.2);
        var farther = new NearbyDriverResult(Guid.NewGuid(), "Farther Driver", "Van", 5.0);
        _repo.Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);
        _driverClient
            .Setup(d => d.GetAvailableNearbyAsync(10, 20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NearbyDriverResult> { nearest, farther });
        var assigned = MakeShipment(id: shipment.Id, customerId: shipment.CustomerId);
        assigned.DriverId = nearest.DriverId;
        _repo.Setup(r => r.AssignDriverAsync(shipment.Id, nearest.DriverId, It.IsAny<CancellationToken>())).ReturnsAsync(assigned);

        var sut = CreateSut();
        var result = await sut.AutoAssignDriverAsync(shipment.Id, radiusKm: 10);

        Assert.Equal(nearest.DriverId, result.DriverId);
    }

    [Fact]
    public async Task AutoAssignDriverAsync_Throws_WhenNoDriverAvailable()
    {
        var shipment = MakeShipment();
        _repo.Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);
        _driverClient
            .Setup(d => d.GetAvailableNearbyAsync(10, 20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NearbyDriverResult>());

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AutoAssignDriverAsync(shipment.Id, radiusKm: 10));
    }

    // ---- Delivery attempts ----

    [Fact]
    public async Task LogDeliveryAttemptAsync_Success_MarksDeliveredAndPublishes()
    {
        var shipment = MakeShipment(status: ShipmentStatus.OutForDelivery);
        var updated = MakeShipment(id: shipment.Id, status: ShipmentStatus.Delivered, customerId: shipment.CustomerId);
        _repo.Setup(r => r.AddDeliveryAttemptAsync(shipment.Id, It.IsAny<DeliveryAttempt>(), ShipmentStatus.Delivered, It.IsAny<ShipmentStatusHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var sut = CreateSut();
        var result = await sut.LogDeliveryAttemptAsync(shipment.Id, new LogDeliveryAttemptRequest(true, null, "Left at door"));

        Assert.NotNull(result);
        Assert.True(result!.Successful);
        Assert.Equal("Delivered", result.ShipmentStatus);
        _publisher.Verify(p => p.PublishShipmentStatusChangedAsync(It.IsAny<BuildingBlocks.IntegrationEvents.ShipmentStatusChangedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogDeliveryAttemptAsync_Failure_MarksFailedDelivery_WithReason()
    {
        var shipment = MakeShipment(status: ShipmentStatus.OutForDelivery);
        var updated = MakeShipment(id: shipment.Id, status: ShipmentStatus.FailedDelivery, customerId: shipment.CustomerId);
        _repo.Setup(r => r.AddDeliveryAttemptAsync(shipment.Id, It.IsAny<DeliveryAttempt>(), ShipmentStatus.FailedDelivery, It.IsAny<ShipmentStatusHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var sut = CreateSut();
        var result = await sut.LogDeliveryAttemptAsync(shipment.Id, new LogDeliveryAttemptRequest(false, nameof(DeliveryAttemptFailureReason.RecipientAbsent), "Nobody home"));

        Assert.NotNull(result);
        Assert.False(result!.Successful);
        Assert.Equal(nameof(DeliveryAttemptFailureReason.RecipientAbsent), result.FailureReason);
    }

    // ---- Ratings ----

    [Fact]
    public async Task AddRatingAsync_Throws_WhenShipmentNotDelivered()
    {
        var shipment = MakeShipment(status: ShipmentStatus.InTransit);
        _repo.Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.AddRatingAsync(shipment.Id, shipment.CustomerId, new CreateRatingRequest(5, "Great!")));
    }

    [Fact]
    public async Task AddRatingAsync_Throws_WhenAlreadyRated()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Delivered);
        _repo.Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);
        _repo.Setup(r => r.GetRatingAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rating { Id = Guid.NewGuid(), ShipmentId = shipment.Id, CustomerId = shipment.CustomerId, Stars = 4, CreatedAt = DateTime.UtcNow });

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.AddRatingAsync(shipment.Id, shipment.CustomerId, new CreateRatingRequest(5, "Great!")));
        _repo.Verify(r => r.AddRatingAsync(It.IsAny<Rating>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddRatingAsync_Succeeds_WhenDeliveredAndNotYetRated()
    {
        var shipment = MakeShipment(status: ShipmentStatus.Delivered);
        _repo.Setup(r => r.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);
        _repo.Setup(r => r.GetRatingAsync(shipment.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Rating?)null);
        _repo.Setup(r => r.AddRatingAsync(It.IsAny<Rating>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rating rating, CancellationToken _) => rating);

        var sut = CreateSut();
        var result = await sut.AddRatingAsync(shipment.Id, shipment.CustomerId, new CreateRatingRequest(5, "Great!"));

        Assert.Equal(5, result.Stars);
        Assert.Equal(shipment.Id, result.ShipmentId);
    }

    // Note: "cannot rate someone else's shipment" is an ownership check enforced by the controller
    // (mirrors GetInvoice's ForbidResult pattern), not the application service — CustomerId is passed
    // in by the controller after that check, so it isn't re-validated here.

    // ---- Bulk create ----

    [Fact]
    public async Task CreateBulkAsync_ContinuesAfterOneFailure_AndReportsPerItemResults()
    {
        var goodPackageId = Guid.NewGuid();
        var badPackageId = Guid.NewGuid();
        var senderId = Guid.NewGuid();

        _packageClient.Setup(p => p.GetPackageAsync(goodPackageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PackageValidationResult(2m, "Standard", senderId));
        _packageClient.Setup(p => p.GetPackageAsync(badPackageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PackageValidationResult?)null);
        _pricing.Setup(p => p.Calculate(It.IsAny<decimal>(), It.IsAny<string>())).Returns(10m);

        var sut = CreateSut();
        var requests = new List<CreateShipmentRequest>
        {
            new(goodPackageId, PickupDto, DeliveryDto),
            new(badPackageId, PickupDto, DeliveryDto)
        };

        var results = await sut.CreateBulkAsync(requests);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Success);
        Assert.NotNull(results[0].Shipment);
        Assert.False(results[1].Success);
        Assert.Null(results[1].Shipment);
        Assert.Contains("not found", results[1].Error, StringComparison.OrdinalIgnoreCase);
    }
}
