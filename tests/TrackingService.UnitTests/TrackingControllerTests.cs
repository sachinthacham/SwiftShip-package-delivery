using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using TrackingService.API.Controllers;
using TrackingService.Application.Abstractions;
using TrackingService.Application.DTOs;

namespace TrackingService.UnitTests;

public class TrackingControllerTests
{
    [Fact]
    public async Task Add_ReturnsCreated_WithRouteToPackage()
    {
        var request = new AddTrackingRequest(Guid.NewGuid(), "Loc", "Status");
        var response = new TrackingResponse(Guid.NewGuid(), request.PackageId, "Loc", "Status", DateTime.UtcNow);
        var mock = new Mock<ITrackingService>();
        mock.Setup(t => t.AddAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = new TrackingController(mock.Object);

        var result = await controller.Add(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TrackingController.GetByPackageId), created.ActionName);
        Assert.Same(response, created.Value);
        var routeValues = Assert.IsType<RouteValueDictionary>(created.RouteValues);
        Assert.Equal(response.PackageId, routeValues["packageId"]);
    }

    [Fact]
    public async Task GetByPackageId_ReturnsOk_WithList()
    {
        var packageId = Guid.NewGuid();
        var list = new List<TrackingResponse>
        {
            new(Guid.NewGuid(), packageId, "L", "S", DateTime.UtcNow)
        }.AsReadOnly();

        var mock = new Mock<ITrackingService>();
        mock.Setup(t => t.GetByPackageIdAsync(packageId, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var controller = new TrackingController(mock.Object);

        var result = await controller.GetByPackageId(packageId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(list, ok.Value);
    }
}
