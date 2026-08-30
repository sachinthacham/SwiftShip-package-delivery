using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using PackageService.Application.Abstractions;
using PackageService.Application.DTOs;
using PackageService.Domain.Enums;
using System.Security.Claims;

namespace PackageService.API.Controllers;

[ApiController]
[Route("api/packages")]
[Authorize]
public class PackagesController : ControllerBase
{
    private readonly IPackageService _service;

    public PackagesController(IPackageService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = Roles.CustomerDispatcherOrAdmin)]
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

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PackageStatus? status = null)
    {
        Guid? senderId = null;
        if (User.IsInRole(Roles.Customer))
        {
            if (!TryGetUserId(out var currentUserId))
            {
                return Unauthorized(new { Message = "User id claim is missing or invalid in token." });
            }

            senderId = currentUserId;
        }

        var result = await _service.GetPagedAsync(page, pageSize, status, senderId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.DispatcherOrAdmin)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdatePackageStatusRequest request)
    {
        var result = await _service.UpdateStatusAsync(id, request.Status);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var package = await _service.GetByIdAsync(id);
        if (package is null) return NotFound();

        if (User.IsInRole(Roles.Customer))
        {
            if (!TryGetUserId(out var currentUserId) || package.SenderId != currentUserId)
            {
                return Forbid();
            }
        }

        var result = await _service.CancelAsync(id);
        return Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(claim, out userId);
    }
}
