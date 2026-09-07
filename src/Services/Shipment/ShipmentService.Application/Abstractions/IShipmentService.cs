using BuildingBlocks.Common;
using ShipmentService.Application.DTOs;
using ShipmentService.Domain.Enums;

namespace ShipmentService.Application.Abstractions;

public interface IShipmentService
{
    Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<ShipmentResponse?> GetByIdAsync(Guid id);
    Task<DeliveryAttemptResponse?> LogDeliveryAttemptAsync(Guid shipmentId, LogDeliveryAttemptRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedList<ShipmentResponse>> GetPagedAsync(int pageNumber, int pageSize, ShipmentStatus? status, Guid? customerId, Guid? driverId, CancellationToken cancellationToken = default);
    Task<ShipmentResponse?> UpdateStatusAsync(Guid id, ShipmentStatus newStatus, CancellationToken cancellationToken = default);
    Task<ShipmentResponse?> AssignDriverAsync(Guid id, Guid driverId, CancellationToken cancellationToken = default);

    Task<DeliveryAttemptResponse?> AttachProofOfDeliveryAsync(
        Guid shipmentId, Guid attemptId, Stream fileContent, string fileName, string contentType, CancellationToken cancellationToken = default);

    Task<InvoiceResponse?> GetInvoiceAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<InvoiceResponse?> UpdatePaymentStatusAsync(Guid shipmentId, PaymentStatus status, CancellationToken cancellationToken = default);

    /// <summary>Starts a Stripe Checkout session for the shipment's invoice. Throws KeyNotFoundException if the shipment or invoice does not exist, InvalidOperationException if the invoice is already paid.</summary>
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(Guid shipmentId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies and processes a Stripe webhook payload, marking the referenced invoice paid on a completed checkout. Throws ArgumentException if the signature is invalid.</summary>
    Task HandleStripeWebhookAsync(string requestBody, string? signatureHeader, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BulkShipmentResult>> CreateBulkAsync(IReadOnlyList<CreateShipmentRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>Finds the nearest available driver within radiusKm of the shipment's pickup address and assigns them. Throws KeyNotFoundException if the shipment does not exist, InvalidOperationException if no driver is available or the pickup address has no coordinates.</summary>
    Task<ShipmentResponse> AutoAssignDriverAsync(Guid shipmentId, double radiusKm, CancellationToken cancellationToken = default);

    Task<ShipmentAnalyticsSummaryResponse> GetAnalyticsSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriverPerformanceResponse>> GetDriverPerformanceAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws KeyNotFoundException if the shipment does not exist, InvalidOperationException if it is not yet Delivered or already rated.</summary>
    Task<RatingResponse> AddRatingAsync(Guid shipmentId, Guid customerId, CreateRatingRequest request, CancellationToken cancellationToken = default);
    Task<RatingResponse?> GetRatingAsync(Guid shipmentId, CancellationToken cancellationToken = default);
}
