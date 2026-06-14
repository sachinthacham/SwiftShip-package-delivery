using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using PackageService.API.Controllers;
using PackageService.Application.Abstractions;
using PackageService.Application.DTOs;

namespace PackageService.UnitTests;

public class PackagesControllerTests
{
    private static void AttachUser(PackagesController controller, string? userIdClaim)
    {
        var httpContext = new DefaultHttpContext();
        if (userIdClaim is not null)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)],
                authenticationType: "Test");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenUserIdClaimMissing()
    {
        var request = new CreatePackageRequest("R", "P", "Addr", 1, 1, 1, 1);
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
        var request = new CreatePackageRequest("R", "P", "Addr", 1, 1, 1, 1);
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
        var request = new CreatePackageRequest("R", "P", "Addr", 1, 1, 1, 1);
        var expected = new PackageResponse(
            Guid.NewGuid(),
            senderId,
            "R",
            "P",
            "Addr",
            1, 1, 1, 1,
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
            "A",
            1, 1, 1, 1,
            "Created",
            DateTime.UtcNow);
        var mock = new Mock<IPackageService>();
        mock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(response);

        var controller = new PackagesController(mock.Object);

        var result = await controller.Get(id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }
}
