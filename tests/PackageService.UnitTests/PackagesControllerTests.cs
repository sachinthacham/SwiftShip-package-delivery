using System.Security.Claims;
using BuildingBlocks.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using PackageService.API.Controllers;
using PackageService.Application.Abstractions;
using PackageService.Application.DTOs;
using PackageService.Domain.Enums;

namespace PackageService.UnitTests;

public class PackagesControllerTests
{
    private static readonly AddressDto TestAddress = new("1 Main St", "City", "State", "00000", "Country");

    private static void AttachUser(PackagesController controller, string? userIdClaim, string? role = null)
    {
        var httpContext = new DefaultHttpContext();
        if (userIdClaim is not null)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userIdClaim) };
            if (role is not null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenUserIdClaimMissing()
    {
        var request = new CreatePackageRequest("R", "P", TestAddress, 1, 1, 1, 1, 100, DeliveryType.Standard);
        var mock = new Mock<IPackageService>();
        var controller = new PackagesController(mock.Object);
        AttachUser(controller, userIdClaim: null);

        var result = await controller.Create(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
        mock.Verify(s => s.CreateAsync(It.IsAny<CreatePackageRequest>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenUserIdIsNotGuid()
    {
        var request = new CreatePackageRequest("R", "P", TestAddress, 1, 1, 1, 1, 100, DeliveryType.Standard);
        var mock = new Mock<IPackageService>();
        var controller = new PackagesController(mock.Object);
        AttachUser(controller, userIdClaim: "not-a-guid");

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
        mock.Verify(s => s.CreateAsync(It.IsAny<CreatePackageRequest>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenTokenHasSubInsteadOfNameIdentifier()
    {
        var senderId = Guid.NewGuid();
        var request = new CreatePackageRequest("R", "P", TestAddress, 1, 1, 1, 1, 100, DeliveryType.Standard);
        var expected = new PackageResponse(
            Guid.NewGuid(),
            senderId,
            "R",
            "P",
            TestAddress,
            1, 1, 1, 1,
            100,
            "Standard",
            "Created",
            DateTime.UtcNow);

        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.CreateAsync(request, senderId)).ReturnsAsync(expected);

        var controller = new PackagesController(mock.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, senderId.ToString())],
            "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Create(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((PackageResponse?)null);

        var controller = new PackagesController(mock.Object);

        var result = await controller.Get(id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenFound()
    {
        var id = Guid.NewGuid();
        var response = new PackageResponse(
            id,
            Guid.NewGuid(),
            "R",
            "P",
            TestAddress,
            1, 1, 1, 1,
            100,
            "Standard",
            "Created",
            DateTime.UtcNow);
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(response);

        var controller = new PackagesController(mock.Object);

        var result = await controller.Get(id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task GetPaged_ForcesSenderIdFilter_WhenCallerIsCustomer()
    {
        var customerId = Guid.NewGuid();
        var expected = new PaginatedList<PackageResponse>([], 0, 1, 20);
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.GetPagedAsync(1, 20, null, customerId)).ReturnsAsync(expected);

        var controller = new PackagesController(mock.Object);
        AttachUser(controller, customerId.ToString(), role: "Customer");

        var result = await controller.GetPaged();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        mock.Verify(s => s.GetPagedAsync(1, 20, null, customerId), Times.Once);
    }

    [Fact]
    public async Task GetPaged_AllowsUnfilteredQuery_ForDispatcher()
    {
        var expected = new PaginatedList<PackageResponse>([], 0, 1, 20);
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.GetPagedAsync(1, 20, PackageStatus.Created, null)).ReturnsAsync(expected);

        var controller = new PackagesController(mock.Object);
        AttachUser(controller, Guid.NewGuid().ToString(), role: "Dispatcher");

        var result = await controller.GetPaged(status: PackageStatus.Created);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task Cancel_ReturnsForbid_WhenCustomerDoesNotOwnPackage()
    {
        var packageId = Guid.NewGuid();
        var response = new PackageResponse(
            packageId, Guid.NewGuid(), "R", "P", TestAddress, 1, 1, 1, 1, 100, "Standard", "Created", DateTime.UtcNow);
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.GetByIdAsync(packageId)).ReturnsAsync(response);

        var controller = new PackagesController(mock.Object);
        AttachUser(controller, Guid.NewGuid().ToString(), role: "Customer");

        var result = await controller.Cancel(packageId);

        Assert.IsType<ForbidResult>(result);
        mock.Verify(s => s.CancelAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ReturnsOk_WhenCustomerOwnsPackage()
    {
        var packageId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var response = new PackageResponse(
            packageId, senderId, "R", "P", TestAddress, 1, 1, 1, 1, 100, "Standard", "Created", DateTime.UtcNow);
        var cancelled = response with { Status = "Cancelled" };
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.GetByIdAsync(packageId)).ReturnsAsync(response);
        mock.Setup(s => s.CancelAsync(packageId)).ReturnsAsync(cancelled);

        var controller = new PackagesController(mock.Object);
        AttachUser(controller, senderId.ToString(), role: "Customer");

        var result = await controller.Cancel(packageId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(cancelled, ok.Value);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_WhenMissing()
    {
        var packageId = Guid.NewGuid();
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.UpdateStatusAsync(packageId, PackageStatus.PickedUp)).ReturnsAsync((PackageResponse?)null);

        var controller = new PackagesController(mock.Object);

        var result = await controller.UpdateStatus(packageId, new UpdatePackageStatusRequest(PackageStatus.PickedUp));

        Assert.IsType<NotFoundResult>(result);
    }
}
