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
    public async Task<IActionResult> Create(CreateDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _driverService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _driverService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/availability")]
    public async Task<IActionResult> SetAvailability(Guid id, SetDriverAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var updated = await _driverService.SetAvailabilityAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}
