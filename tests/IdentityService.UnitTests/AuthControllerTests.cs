using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdentityService.UnitTests;

public class AuthControllerTests
{
    private static AuthController CreateControllerWithUser(IAuthService authService, Guid userId)
    {
        var controller = new AuthController(authService);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

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

    [Fact]
    public async Task Me_ReturnsOk_WithCurrentUser_FromTokenSubject()
    {
        var userId = Guid.NewGuid();
        var response = new MeResponse(userId, "a@b.com", "A", "B", "Customer", DateTime.UtcNow);
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.GetCurrentUser(userId)).ReturnsAsync(response);

        var controller = CreateControllerWithUser(mock.Object, userId);

        var result = await controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task ChangePassword_ReturnsNoContent_WhenServiceCompletes()
    {
        var userId = Guid.NewGuid();
        var request = new ChangePasswordRequest("OldPassword1!", "NewPassword1!");
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.ChangePassword(userId, request)).Returns(Task.CompletedTask);

        var controller = CreateControllerWithUser(mock.Object, userId);

        var result = await controller.ChangePassword(request);

        Assert.IsType<NoContentResult>(result);
        mock.Verify(a => a.ChangePassword(userId, request), Times.Once);
    }
}
