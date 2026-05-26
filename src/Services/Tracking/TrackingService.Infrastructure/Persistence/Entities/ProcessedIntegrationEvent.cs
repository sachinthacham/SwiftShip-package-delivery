namespace TrackingService.Infrastructure.Persistence.Entities;

public class ProcessedIntegrationEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public string EventKey { get; set; } = default!;
    public DateTime ProcessedAtUtc { get; set; }
}
