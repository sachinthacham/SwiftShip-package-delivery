using BuildingBlocks.Authorization;
using DriverService.API.Extensions;
using DriverService.Application.Abstractions;
using DriverService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriverService.API.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriversController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpPost]
    [Authorize(Roles = Roles.DispatcherOrAdmin)]
    public async Task<IActionResult> Create(CreateDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _driverService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = Roles.DispatcherOrAdmin)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isAvailable = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _driverService.GetPagedAsync(page, pageSize, isAvailable, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.DispatcherOrAdmin)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _driverService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Roles = Roles.Courier)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new { Message = "User id claim is missing or invalid in token." });
        }

        var result = await _driverService.GetByUserIdAsync(userId, cancellationToken);
        return result is null ? NotFound(new { Message = "No driver profile is linked to this account." }) : Ok(result);
    }

    [HttpPut("{id:guid}/availability")]
    [Authorize(Roles = Roles.CourierOrAdmin)]
    public async Task<IActionResult> SetAvailability(Guid id, SetDriverAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var updated = await _driverService.SetAvailabilityAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpPut("me/location")]
    [Authorize(Roles = Roles.Courier)]
    public async Task<IActionResult> UpdateMyLocation(UpdateDriverLocationRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new { Message = "User id claim is missing or invalid in token." });
        }

        var driver = await _driverService.GetByUserIdAsync(userId, cancellationToken);
        if (driver is null)
        {
            return NotFound(new { Message = "No driver profile is linked to this account." });
        }

        await _driverService.UpdateLocationAsync(driver.Id, request, cancellationToken);
        return NoContent();
    }
}
