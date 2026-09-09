using ShipmentService.Domain.Enums;

namespace ShipmentService.Domain.Entities;

/// <summary>
/// One invoice per shipment, generated at creation time. PaymentStatus is tracked manually today
/// (via Dispatcher/Admin) — a future Stripe (or similar) integration would update it from webhooks
/// instead, without changing this shape.
/// </summary>
public class Invoice
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid CustomerId { get; set; }

    public string InvoiceNumber { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>Stripe Checkout Session id, set once a checkout session is started for this invoice.</summary>
    public string? StripeSessionId { get; set; }
}
