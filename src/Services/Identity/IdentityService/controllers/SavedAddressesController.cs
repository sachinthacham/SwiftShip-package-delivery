using IdentityService.API.Extensions;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/me/addresses")]
[Authorize]
public class SavedAddressesController : ControllerBase
{
    private readonly ISavedAddressService _addresses;

    public SavedAddressesController(ISavedAddressService addresses)
    {
        _addresses = addresses;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSavedAddressRequest request)
    {
        var result = await _addresses.Create(User.GetUserId(), request);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _addresses.GetForUser(User.GetUserId());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _addresses.GetById(User.GetUserId(), id);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSavedAddressRequest request)
    {
        var result = await _addresses.Update(User.GetUserId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _addresses.Delete(User.GetUserId(), id);
        return NoContent();
    }

    [HttpPut("{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        var result = await _addresses.SetDefault(User.GetUserId(), id);
        return Ok(result);
    }
}
