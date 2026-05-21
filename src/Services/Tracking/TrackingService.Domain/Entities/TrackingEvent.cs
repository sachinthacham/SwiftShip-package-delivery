namespace TrackingService.Domain.Entities;

public class TrackingEvent
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string Location { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime TimestampUtc { get; set; }
}
