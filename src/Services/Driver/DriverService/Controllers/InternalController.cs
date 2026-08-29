using DriverService.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DriverService.API.Controllers;

/// <summary>
/// Service-to-service endpoints used by other backend services (never exposed to the Angular client).
/// Authenticated with a shared secret instead of a user JWT since callers are services, not users.
/// </summary>
[ApiController]
[Route("api/internal/drivers")]
public class InternalController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly IConfiguration _configuration;

    public InternalController(IDriverService driverService, IConfiguration configuration)
    {
        _driverService = driverService;
        _configuration = configuration;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        CancellationToken cancellationToken)
    {
        var expectedKey = _configuration["Internal:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey) || apiKey != expectedKey)
        {
            return Unauthorized();
        }

        var result = await _driverService.GetAvailableNearbyAsync(lat, lng, radiusKm, cancellationToken);
        return Ok(result);
    }
}
