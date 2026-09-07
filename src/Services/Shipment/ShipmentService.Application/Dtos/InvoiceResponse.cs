namespace ShipmentService.Application.DTOs;

public record InvoiceResponse(
    Guid Id,
    Guid ShipmentId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    string PaymentStatus,
    DateTime IssuedAt,
    DateTime? PaidAt
);
