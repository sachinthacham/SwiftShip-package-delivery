namespace ShipmentService.Domain.Entities;

public class ShipmentStatusHistory
{
    public Guid Id { get; set; }

    public Guid ShipmentId { get; set; }

    public string Status { get; set; } = default!;
    public string Location { get; set; } = default!;

    public DateTime Timestamp { get; set; }
}