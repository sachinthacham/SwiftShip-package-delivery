namespace ShipmentService.Infrastructure.Persistence.Entities;

public class ShipmentRequestIdempotency
{
    public string IdempotencyKey { get; set; } = default!;
    public Guid? ShipmentId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
