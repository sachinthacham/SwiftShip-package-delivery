using BuildingBlocks.Exceptions;
using ShipmentService.Application.Abstractions;

namespace ShipmentService.Infrastructure.Payments;

/// <summary>Fallback used when Stripe:SecretKey is not configured — the manual payment-status endpoint remains available.</summary>
public class NullPaymentGateway : IPaymentGateway
{
    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid invoiceId, Guid shipmentId, decimal amount, string currency,
        string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        throw new ConflictException("Online payment is not configured for this environment. Use the manual payment-status endpoint instead.");
    }

    public Guid? VerifyAndParseCheckoutCompleted(string requestBody, string? signatureHeader)
    {
        throw new ArgumentException("Stripe is not configured; webhook cannot be processed.");
    }
}
