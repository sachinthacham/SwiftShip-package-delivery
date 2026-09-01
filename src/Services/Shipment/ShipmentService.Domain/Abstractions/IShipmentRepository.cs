using ShipmentService.Domain.Entities;
using ShipmentService.Domain.Enums;
using ShipmentService.Domain.Models;

namespace ShipmentService.Domain.Abstractions;

public interface IShipmentRepository
{
    Task AddAsync(Shipment shipment, ShipmentStatusHistory initialStatusHistory, Invoice invoice, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetShipmentIdByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<bool> TryReserveIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SetIdempotencyResultAsync(string idempotencyKey, Guid shipmentId, CancellationToken cancellationToken = default);
    Task ReleaseIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Records a delivery attempt, updates the shipment's status, and appends a status history entry in one transaction. Returns null if the shipment does not exist.</summary>
    Task<Shipment?> AddDeliveryAttemptAsync(
        Guid shipmentId,
        DeliveryAttempt attempt,
        ShipmentStatus newStatus,
        ShipmentStatusHistory historyEntry,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the shipment's status and appends a status history entry. Returns null if the shipment does not exist.</summary>
    Task<Shipment?> UpdateStatusAsync(
        Guid shipmentId,
        ShipmentStatus newStatus,
        ShipmentStatusHistory historyEntry,
        CancellationToken cancellationToken = default);

    /// <summary>Assigns a courier to the shipment. Returns null if the shipment does not exist.</summary>
    Task<Shipment?> AssignDriverAsync(Guid shipmentId, Guid driverId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Shipment> Items, int TotalCount, int PageNumber, int PageSize)> GetPagedAsync(
        int pageNumber, int pageSize, ShipmentStatus? status, Guid? customerId, Guid? driverId,
        CancellationToken cancellationToken = default);

    Task<DeliveryAttempt?> GetDeliveryAttemptAsync(Guid shipmentId, Guid attemptId, CancellationToken cancellationToken = default);
    Task SetDeliveryAttemptProofUrlAsync(Guid attemptId, string proofOfDeliveryUrl, CancellationToken cancellationToken = default);

    Task<Invoice?> GetInvoiceByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<Invoice?> UpdatePaymentStatusAsync(Guid shipmentId, PaymentStatus status, CancellationToken cancellationToken = default);

    Task SetInvoiceStripeSessionIdAsync(Guid invoiceId, string stripeSessionId, CancellationToken cancellationToken = default);

    /// <summary>Marks the invoice Paid and sets PaidAt. Idempotent — returns the invoice unchanged if it was already Paid. Returns null if the invoice does not exist.</summary>
    Task<Invoice?> MarkInvoicePaidAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>Shipments not yet Delivered/Cancelled and not yet flagged as SLA-breached, for the SLA monitor to scan.</summary>
    Task<IReadOnlyList<Shipment>> GetActiveForSlaCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks a shipment as SLA-breached (once). Returns false if it was already breached or does not exist, so the caller does not re-publish.</summary>
    Task<bool> TryMarkSlaBreachedAsync(Guid shipmentId, DateTime breachedAtUtc, CancellationToken cancellationToken = default);

    Task<ShipmentAnalyticsSummary> GetAnalyticsSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriverPerformanceSummary>> GetDriverPerformanceAsync(CancellationToken cancellationToken = default);

    Task<Rating?> GetRatingAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<Rating> AddRatingAsync(Rating rating, CancellationToken cancellationToken = default);
}
