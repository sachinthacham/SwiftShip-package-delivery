namespace ShipmentService.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }

    public Guid PackageId { get; set; }

    public string TrackingNumber { get; set; } = default!;

    public Guid? DriverId { get; set; }

    public string Status { get; set; } = "Created";

    public string PickupAddress { get; set; } = default!;
    public string DeliveryAddress { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ShipmentStatusHistory> StatusHistory { get; set; } = new();
}