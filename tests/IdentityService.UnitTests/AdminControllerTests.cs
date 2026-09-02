using BuildingBlocks.Common;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdentityService.UnitTests;

public class AdminControllerTests
{
    [Fact]
    public async Task CreateUser_ReturnsOk_WhenServiceCompletes()
    {
        var request = new AdminCreateUserRequest("dispatcher@a.com", "Password1!", "A", "B", "Dispatcher");
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.CreateUserAsAdmin(request)).Returns(Task.CompletedTask);

        var controller = new AdminController(mock.Object);

        var result = await controller.CreateUser(request);

        Assert.IsType<OkResult>(result);
        mock.Verify(a => a.CreateUserAsAdmin(request), Times.Once);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WithPagedResult()
    {
        var items = new List<UserSummaryResponse>
        {
            new(Guid.NewGuid(), "a@b.com", "A", "B", "Customer", DateTime.UtcNow)
        };
        var page = new PaginatedList<UserSummaryResponse>(items, totalCount: 1, pageNumber: 1, pageSize: 20);
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.GetUsers(1, 20)).ReturnsAsync(page);

        var controller = new AdminController(mock.Object);

        var result = await controller.GetUsers(1, 20);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(page, ok.Value);
    }
}
