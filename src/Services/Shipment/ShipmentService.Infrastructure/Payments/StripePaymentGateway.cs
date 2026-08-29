using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShipmentService.Application.Abstractions;
using Stripe;
using Stripe.Checkout;

namespace ShipmentService.Infrastructure.Payments;

/// <summary>Real Stripe Checkout integration, active when Stripe:SecretKey is configured.</summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly string _webhookSecret;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(IConfiguration configuration, ILogger<StripePaymentGateway> logger)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid invoiceId, Guid shipmentId, decimal amount, string currency,
        string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency.ToLowerInvariant(),
                        UnitAmount = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Shipment invoice {invoiceId}"
                        }
                    }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["invoiceId"] = invoiceId.ToString(),
                ["shipmentId"] = shipmentId.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return new CheckoutSessionResult(session.Id, session.Url);
    }

    public Guid? VerifyAndParseCheckoutCompleted(string requestBody, string? signatureHeader)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(requestBody, signatureHeader, _webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            throw new ArgumentException("Invalid Stripe webhook signature.", ex);
        }

        if (stripeEvent.Type != "checkout.session.completed" || stripeEvent.Data.Object is not Session session)
        {
            return null;
        }

        if (session.Metadata is not null
            && session.Metadata.TryGetValue("invoiceId", out var invoiceIdString)
            && Guid.TryParse(invoiceIdString, out var invoiceId))
        {
            return invoiceId;
        }

        _logger.LogWarning("Stripe checkout.session.completed event {SessionId} had no invoiceId metadata.", session.Id);
        return null;
    }
}
