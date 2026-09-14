using BuildingBlocks.Authorization;
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
    [Authorize(Roles = Roles.CourierDispatcherOrAdmin)]
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

    /// <summary>Guest tracking lookup by customer-facing tracking number — no authentication required.</summary>
    [HttpGet("public/{trackingNumber}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByTrackingNumber(string trackingNumber, CancellationToken cancellationToken)
    {
        var result = await _trackingService.GetByTrackingNumberAsync(trackingNumber, cancellationToken);
        return result.Count == 0 ? NotFound() : Ok(result);
    }
}
