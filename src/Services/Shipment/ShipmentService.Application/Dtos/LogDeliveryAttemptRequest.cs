namespace ShipmentService.Application.DTOs;

public record LogDeliveryAttemptRequest(
    bool Successful,
    string? FailureReason,
    string? Notes
);
