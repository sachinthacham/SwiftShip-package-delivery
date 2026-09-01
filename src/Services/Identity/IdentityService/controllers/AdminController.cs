using BuildingBlocks.Authorization;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IAuthService _auth;

    public AdminController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(AdminCreateUserRequest request)
    {
        await _auth.CreateUserAsAdmin(request);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _auth.GetUsers(page, pageSize);
        return Ok(result);
    }
}
