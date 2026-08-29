namespace ShipmentService.Application.Abstractions;

public record CheckoutSessionResult(string SessionId, string CheckoutUrl);

public interface IPaymentGateway
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid invoiceId, Guid shipmentId, decimal amount, string currency,
        string successUrl, string cancelUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the webhook signature and, if valid, returns the InvoiceId embedded in a completed
    /// checkout session's metadata (null if the signature is valid but the event isn't a completed
    /// checkout, or has no invoiceId metadata). Throws ArgumentException if the signature is invalid.
    /// </summary>
    Guid? VerifyAndParseCheckoutCompleted(string requestBody, string? signatureHeader);
}
