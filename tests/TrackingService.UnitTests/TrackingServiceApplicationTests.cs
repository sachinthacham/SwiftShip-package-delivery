using Moq;
using TrackingService.Application.DTOs;
using TrackingService.Domain.Abstractions;
using TrackingService.Domain.Entities;
using TrackingServiceImpl = TrackingService.Application.Services.TrackingService;

namespace TrackingService.UnitTests;

public class TrackingServiceApplicationTests
{
    [Fact]
    public async Task AddAsync_PersistsEvent_AndReturnsMappedResponse()
    {
        var request = new AddTrackingRequest(Guid.NewGuid(), "Warehouse", "PickedUp");
        TrackingEvent? captured = null;
        var mock = new Mock<ITrackingRepository>();
        mock.Setup(r => r.AddAsync(It.IsAny<TrackingEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TrackingEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var service = new TrackingServiceImpl(mock.Object);

        var result = await service.AddAsync(request, CancellationToken.None);

        mock.Verify(r => r.AddAsync(It.IsAny<TrackingEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(request.PackageId, captured!.PackageId);
        Assert.Equal(request.Location, captured.Location);
        Assert.Equal(request.Status, captured.Status);
        Assert.Equal(request.PackageId, result.PackageId);
        Assert.Equal(request.Location, result.Location);
    }

    [Fact]
    public async Task GetByPackageIdAsync_ReturnsMappedHistory_InRepositoryOrder()
    {
        var packageId = Guid.NewGuid();
        var history = new List<TrackingEvent>
        {
            new() { Id = Guid.NewGuid(), PackageId = packageId, Location = "A", Status = "Created", TimestampUtc = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PackageId = packageId, Location = "B", Status = "PickedUp", TimestampUtc = DateTime.UtcNow }
        };
        var mock = new Mock<ITrackingRepository>();
        mock.Setup(r => r.GetByPackageIdAsync(packageId, It.IsAny<CancellationToken>())).ReturnsAsync(history);

        var service = new TrackingServiceImpl(mock.Object);

        var result = await service.GetByPackageIdAsync(packageId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Location);
        Assert.Equal("B", result[1].Location);
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_ReturnsEmptyList_WhenNoEvents()
    {
        var mock = new Mock<ITrackingRepository>();
        mock.Setup(r => r.GetByTrackingNumberAsync("TRK-MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackingEvent>());

        var service = new TrackingServiceImpl(mock.Object);

        var result = await service.GetByTrackingNumberAsync("TRK-MISSING", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_MapsShipmentIdAndTrackingNumber_WhenPresent()
    {
        var trackingNumber = "TRK-1";
        var shipmentId = Guid.NewGuid();
        var history = new List<TrackingEvent>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                ShipmentId = shipmentId,
                TrackingNumber = trackingNumber,
                Location = "Hub",
                Status = "InTransit",
                TimestampUtc = DateTime.UtcNow
            }
        };
        var mock = new Mock<ITrackingRepository>();
        mock.Setup(r => r.GetByTrackingNumberAsync(trackingNumber, It.IsAny<CancellationToken>())).ReturnsAsync(history);

        var service = new TrackingServiceImpl(mock.Object);

        var result = await service.GetByTrackingNumberAsync(trackingNumber, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(shipmentId, result[0].ShipmentId);
        Assert.Equal(trackingNumber, result[0].TrackingNumber);
    }
}
