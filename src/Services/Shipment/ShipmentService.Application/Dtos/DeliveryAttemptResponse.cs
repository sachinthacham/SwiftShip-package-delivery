namespace ShipmentService.Application.DTOs;

public record DeliveryAttemptResponse(
    Guid Id,
    Guid ShipmentId,
    bool Successful,
    string? FailureReason,
    string? Notes,
    string? ProofOfDeliveryUrl,
    DateTime AttemptedAt,
    string ShipmentStatus
);
