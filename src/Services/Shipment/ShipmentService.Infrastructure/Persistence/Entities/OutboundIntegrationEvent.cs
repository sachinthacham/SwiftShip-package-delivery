namespace ShipmentService.Infrastructure.Persistence.Entities;

public class OutboundIntegrationEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public string EventKey { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
