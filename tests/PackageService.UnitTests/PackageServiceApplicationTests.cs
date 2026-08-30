using Moq;
using PackageService.Application.DTOs;
using PackageService.Domain.Abstractions;
using PackageService.Domain.Entities;
using PackageService.Domain.Enums;
using PackageServiceImpl = PackageService.Application.Services.PackageService;

namespace PackageService.UnitTests;

public class PackageServiceApplicationTests
{
    private static readonly AddressDto TestAddress = new("1 Main St", "City", "State", "00000", "Country");

    private static CreatePackageRequest BuildRequest() =>
        new("Receiver", "555-0100", TestAddress, 5, 10, 10, 10, 100, DeliveryType.Standard);

    [Fact]
    public async Task CreateAsync_PersistsPackage_WithCreatedStatus()
    {
        var senderId = Guid.NewGuid();
        Package? captured = null;
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.AddAsync(It.IsAny<Package>()))
            .Callback<Package>(p => captured = p)
            .Returns(Task.CompletedTask);

        var service = new PackageServiceImpl(mock.Object);

        var result = await service.CreateAsync(BuildRequest(), senderId);

        mock.Verify(r => r.AddAsync(It.IsAny<Package>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(senderId, captured!.SenderId);
        Assert.Equal(PackageStatus.Created, captured.Status);
        Assert.Equal("Created", result.Status);
        Assert.Equal(senderId, result.SenderId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Package?)null);
        var service = new PackageServiceImpl(mock.Object);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedResponse_WhenFound()
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverName = "Receiver",
            ReceiverPhone = "555-0100",
            ReceiverAddress = new()
            {
                Street = "1 Main St",
                City = "City",
                State = "State",
                PostalCode = "00000",
                Country = "Country"
            },
            Weight = 5,
            Length = 10,
            Width = 10,
            Height = 10,
            DeclaredValue = 100,
            DeliveryType = DeliveryType.Express,
            Status = PackageStatus.InTransit,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.GetByIdAsync(package.Id)).ReturnsAsync(package);
        var service = new PackageServiceImpl(mock.Object);

        var result = await service.GetByIdAsync(package.Id);

        Assert.NotNull(result);
        Assert.Equal(package.Id, result!.Id);
        Assert.Equal("Express", result.DeliveryType);
        Assert.Equal("InTransit", result.Status);
    }

    [Fact]
    public async Task GetPagedAsync_MapsRepositoryPageToResponsePage()
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverName = "Receiver",
            ReceiverPhone = "555-0100",
            ReceiverAddress = new()
            {
                Street = "1 Main St",
                City = "City",
                State = "State",
                PostalCode = "00000",
                Country = "Country"
            },
            DeliveryType = DeliveryType.Standard,
            Status = PackageStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.GetPagedAsync(1, 20, null, null))
            .ReturnsAsync((new List<Package> { package }, 1, 1, 20));
        var service = new PackageServiceImpl(mock.Object);

        var result = await service.GetPagedAsync(1, 20, null, null);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(package.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNull_WhenPackageMissing()
    {
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Package?)null);
        var service = new PackageServiceImpl(mock.Object);

        var result = await service.UpdateStatusAsync(Guid.NewGuid(), PackageStatus.PickedUp);

        Assert.Null(result);
        mock.Verify(r => r.UpdateAsync(It.IsAny<Package>()), Times.Never);
    }

    [Theory]
    [InlineData(PackageStatus.Delivered)]
    [InlineData(PackageStatus.Cancelled)]
    public async Task UpdateStatusAsync_Throws_WhenPackageAlreadyInTerminalState(PackageStatus terminalStatus)
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverName = "Receiver",
            ReceiverPhone = "555-0100",
            ReceiverAddress = new()
            {
                Street = "1 Main St",
                City = "City",
                State = "State",
                PostalCode = "00000",
                Country = "Country"
            },
            DeliveryType = DeliveryType.Standard,
            Status = terminalStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.GetByIdAsync(package.Id)).ReturnsAsync(package);
        var service = new PackageServiceImpl(mock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStatusAsync(package.Id, PackageStatus.InTransit));

        mock.Verify(r => r.UpdateAsync(It.IsAny<Package>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesAndPersists_WhenTransitionIsValid()
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverName = "Receiver",
            ReceiverPhone = "555-0100",
            ReceiverAddress = new()
            {
                Street = "1 Main St",
                City = "City",
                State = "State",
                PostalCode = "00000",
                Country = "Country"
            },
            DeliveryType = DeliveryType.Standard,
            Status = PackageStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.GetByIdAsync(package.Id)).ReturnsAsync(package);
        mock.Setup(r => r.UpdateAsync(It.IsAny<Package>())).Returns(Task.CompletedTask);
        var service = new PackageServiceImpl(mock.Object);

        var result = await service.UpdateStatusAsync(package.Id, PackageStatus.PickedUp);

        Assert.NotNull(result);
        Assert.Equal("PickedUp", result!.Status);
        mock.Verify(r => r.UpdateAsync(It.Is<Package>(p => p.Status == PackageStatus.PickedUp)), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_DelegatesToUpdateStatusAsync_WithCancelledStatus()
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverName = "Receiver",
            ReceiverPhone = "555-0100",
            ReceiverAddress = new()
            {
                Street = "1 Main St",
                City = "City",
                State = "State",
                PostalCode = "00000",
                Country = "Country"
            },
            DeliveryType = DeliveryType.Standard,
            Status = PackageStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var mock = new Mock<IPackageRepository>();
        mock.Setup(r => r.GetByIdAsync(package.Id)).ReturnsAsync(package);
        mock.Setup(r => r.UpdateAsync(It.IsAny<Package>())).Returns(Task.CompletedTask);
        var service = new PackageServiceImpl(mock.Object);

        var result = await service.CancelAsync(package.Id);

        Assert.NotNull(result);
        Assert.Equal("Cancelled", result!.Status);
    }
}
