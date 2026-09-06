using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShipmentService.Application.Abstractions;

namespace ShipmentService.API.Controllers;

/// <summary>Receives payment-provider webhooks. Not scoped under api/shipments since it isn't shipment-id-addressed and carries no user auth.</summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IShipmentService shipmentService, ILogger<PaymentsController> logger)
    {
        _shipmentService = shipmentService;
        _logger = logger;
    }

    /// <summary>Stripe webhook endpoint. Verifies the Stripe-Signature header and marks the referenced invoice paid on checkout.session.completed.</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        try
        {
            await _shipmentService.HandleStripeWebhookAsync(body, signature, cancellationToken);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Rejected Stripe webhook: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }
}
