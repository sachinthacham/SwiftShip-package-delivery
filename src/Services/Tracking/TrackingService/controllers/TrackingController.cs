using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrackingService.Application.Abstractions;
using TrackingService.Application.DTOs;

namespace TrackingService.API.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;

    public TrackingController(ITrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddTrackingRequest request, CancellationToken cancellationToken)
    {
        var result = await _trackingService.AddAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByPackageId), new { packageId = result.PackageId }, result);
    }

    [HttpGet("{packageId:guid}")]
    public async Task<IActionResult> GetByPackageId(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetByPackageIdAsync(packageId, cancellationToken);
        return Ok(result);
    }
}
