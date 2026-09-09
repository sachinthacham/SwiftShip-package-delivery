using ShipmentService.Domain.Enums;

namespace ShipmentService.Domain.Entities;

public class DeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }

    public bool Successful { get; set; }
    public DeliveryAttemptFailureReason? FailureReason { get; set; }
    public string? Notes { get; set; }
    public string? ProofOfDeliveryUrl { get; set; }

    public DateTime AttemptedAt { get; set; }
}
