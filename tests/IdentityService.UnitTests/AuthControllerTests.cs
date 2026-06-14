using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdentityService.UnitTests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsOk_WhenServiceCompletes()
    {
        var request = new RegisterRequest("a@b.com", "Password1!", "A", "B");
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.Register(request)).Returns(Task.CompletedTask);

        var controller = new AuthController(mock.Object);

        var result = await controller.Register(request);

        Assert.IsType<OkResult>(result);
        mock.Verify(a => a.Register(request), Times.Once);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithAuthResponse()
    {
        var request = new LoginRequest("a@b.com", "Password1!");
        var response = new AuthResponse("access-token", "refresh-token");
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.Login(request)).ReturnsAsync(response);

        var controller = new AuthController(mock.Object);

        var result = await controller.Login(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }
}
