using ShipmentService.Domain.Enums;

namespace ShipmentService.Domain.Entities;

public class ShipmentStatusHistory
{
    public Guid Id { get; set; }

    public Guid ShipmentId { get; set; }

    public ShipmentStatus Status { get; set; }
    public string Location { get; set; } = default!;

    public DateTime Timestamp { get; set; }
}