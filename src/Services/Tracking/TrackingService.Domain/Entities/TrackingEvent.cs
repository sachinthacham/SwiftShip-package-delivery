namespace TrackingService.Domain.Entities;

public class TrackingEvent
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }

    /// <summary>Set for events raised from the shipment lifecycle (creation, status changes); null for older/manual package-only entries.</summary>
    public Guid? ShipmentId { get; set; }

    /// <summary>The customer-facing tracking number, used for public/guest lookups. Null for entries that predate a linked shipment.</summary>
    public string? TrackingNumber { get; set; }

    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime TimestampUtc { get; set; }
}
