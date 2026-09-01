using IdentityService.API.Extensions;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        await _auth.Register(request);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _auth.Login(request);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var result = await _auth.Refresh(request);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        await _auth.Logout(request);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var result = await _auth.GetCurrentUser(User.GetUserId());
        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await _auth.ChangePassword(User.GetUserId(), request);
        return NoContent();
    }
}