using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.DTOs;
using ShipmentService.Domain.Enums;

namespace ShipmentService.API.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpPost]
    [Authorize(Roles = Roles.CustomerDispatcherOrAdmin)]
    public async Task<IActionResult> Create(
        CreateShipmentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _shipmentService.CreateAsync(request, idempotencyKey, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Creates multiple shipments in one request. Each item succeeds or fails independently.</summary>
    [HttpPost("bulk")]
    [Authorize(Roles = Roles.CustomerDispatcherOrAdmin)]
    public async Task<IActionResult> CreateBulk(List<CreateShipmentRequest> requests, CancellationToken cancellationToken)
    {
        var results = await _shipmentService.CreateBulkAsync(requests, cancellationToken);
        return Ok(results);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ShipmentStatus? status = null,
        [FromQuery] Guid? driverId = null,
        CancellationToken cancellationToken = default)
    {
        if (User.IsInRole(Roles.Customer))
        {
            if (!TryGetUserId(out var customerId))
            {
                return Unauthorized(new { message = "User id claim is missing or invalid in token." });
            }

            var ownResult = await _shipmentService.GetPagedAsync(page, pageSize, status, customerId, null, cancellationToken);
            return Ok(ownResult);
        }

        if (User.IsInRole(Roles.Courier))
        {
            if (driverId is null)
            {
                return BadRequest(new { message = "driverId is required for Courier accounts (see GET /api/drivers/me)." });
            }

            var assignedResult = await _shipmentService.GetPagedAsync(page, pageSize, status, null, driverId, cancellationToken);
            return Ok(assignedResult);
        }

        var result = await _shipmentService.GetPagedAsync(page, pageSize, status, null, driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _shipmentService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.CourierDispatcherOrAdmin)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateShipmentStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _shipmentService.UpdateStatusAsync(id, request.Status, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = Roles.DispatcherOrAdmin)]
    public async Task<IActionResult> AssignDriver(Guid id, AssignDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _shipmentService.AssignDriverAsync(id, request.DriverId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Finds the nearest available courier within radiusKm of the pickup address and assigns them.</summary>
    [HttpPost("{id:guid}/auto-assign")]
    [Authorize(Roles = Roles.DispatcherOrAdmin)]
    public async Task<IActionResult> AutoAssignDriver(Guid id, [FromQuery] double radiusKm = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shipmentService.AutoAssignDriverAsync(id, radiusKm, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/attempts")]
    [Authorize(Roles = Roles.CourierDispatcherOrAdmin)]
    public async Task<IActionResult> LogDeliveryAttempt(Guid id, LogDeliveryAttemptRequest request, CancellationToken cancellationToken)
    {
        var result = await _shipmentService.LogDeliveryAttemptAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/attempts/{attemptId:guid}/proof")]
    [Authorize(Roles = Roles.CourierDispatcherOrAdmin)]
    [RequestSizeLimit(MaxProofFileBytes)]
    public async Task<IActionResult> UploadProofOfDelivery(Guid id, Guid attemptId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A file is required." });
        }

        if (file.Length > MaxProofFileBytes)
        {
            return BadRequest(new { message = "File exceeds the 10 MB limit." });
        }

        if (string.IsNullOrEmpty(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only image uploads are supported for proof of delivery." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _shipmentService.AttachProofOfDeliveryAsync(id, attemptId, stream, file.FileName, file.ContentType, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/invoice")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentService.GetByIdAsync(id);
        if (shipment is null) return NotFound();

        if (User.IsInRole(Roles.Customer))
        {
            if (!TryGetUserId(out var currentUserId) || shipment.CustomerId != currentUserId)
            {
                return Forbid();
            }
        }

        var invoice = await _shipmentService.GetInvoiceAsync(id, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    /// <summary>Starts a Stripe Checkout session to pay the shipment's invoice online. Falls back to 409 if Stripe is not configured — use the manual payment-status endpoint instead.</summary>
    [HttpPost("{id:guid}/invoice/checkout-session")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> CreateCheckoutSession(Guid id, CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var customerId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid in token." });
        }

        var shipment = await _shipmentService.GetByIdAsync(id);
        if (shipment is null) return NotFound();
        if (shipment.CustomerId != customerId) return Forbid();

        try
        {
            var result = await _shipmentService.CreateCheckoutSessionAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/invoice/payment-status")]
    [Authorize(Roles = Roles.DispatcherOrAdmin)]
    public async Task<IActionResult> UpdatePaymentStatus(Guid id, UpdatePaymentStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _shipmentService.UpdatePaymentStatusAsync(id, request.Status, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Rates a delivered shipment. Only the owning customer may rate, and only once.</summary>
    [HttpPost("{id:guid}/rating")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> AddRating(Guid id, CreateRatingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var customerId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid in token." });
        }

        var shipment = await _shipmentService.GetByIdAsync(id);
        if (shipment is null) return NotFound();
        if (shipment.CustomerId != customerId) return Forbid();

        try
        {
            var result = await _shipmentService.AddRatingAsync(id, customerId, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/rating")]
    public async Task<IActionResult> GetRating(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentService.GetByIdAsync(id);
        if (shipment is null) return NotFound();

        if (User.IsInRole(Roles.Customer))
        {
            if (!TryGetUserId(out var currentUserId) || shipment.CustomerId != currentUserId)
            {
                return Forbid();
            }
        }

        var rating = await _shipmentService.GetRatingAsync(id, cancellationToken);
        return rating is null ? NotFound() : Ok(rating);
    }

    private const long MaxProofFileBytes = 10_000_000;

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(claim, out userId);
    }
}
