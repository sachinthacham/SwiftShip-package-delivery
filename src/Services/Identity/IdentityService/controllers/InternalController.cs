using IdentityService.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace IdentityService.API.Controllers;

/// <summary>
/// Service-to-service endpoints used by other backend services (never exposed to the Angular client).
/// Authenticated with a shared secret instead of a user JWT since callers are services, not users.
/// </summary>
[ApiController]
[Route("api/internal/users")]
public class InternalController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IConfiguration _configuration;

    public InternalController(IAuthService auth, IConfiguration configuration)
    {
        _auth = auth;
        _configuration = configuration;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey)
    {
        var expectedKey = _configuration["Internal:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey) || apiKey != expectedKey)
        {
            return Unauthorized();
        }

        var result = await _auth.GetCurrentUser(id);
        return Ok(result);
    }
}
