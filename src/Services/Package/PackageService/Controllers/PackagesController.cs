using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using PackageService.Application.Abstractions;
using PackageService.Application.DTOs;
using System.Security.Claims;

namespace PackageService.API.Controllers;

[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly IPackageService _service;

    public PackagesController(IPackageService service)
    {
        _service = service;
    }

   

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreatePackageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { Message = "User id claim is missing in token." });
        }

        if (!Guid.TryParse(userId, out var senderId))
        {
            return BadRequest(new { Message = "Invalid user id in token." });
        }

        var result = await _service.CreateAsync(request, senderId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();

        return Ok(result);
    }

   
}