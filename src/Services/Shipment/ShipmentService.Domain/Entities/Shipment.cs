using ShipmentService.Domain.Enums;
using ShipmentService.Domain.ValueObjects;

namespace ShipmentService.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }

    public Guid PackageId { get; set; }

    /// <summary>The Identity Service User.Id of the customer who owns the underlying package (denormalized from PackageService at creation time).</summary>
    public Guid CustomerId { get; set; }

    public string TrackingNumber { get; set; } = default!;

    public Guid? DriverId { get; set; }

    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;

    /// <summary>Denormalized from PackageService at creation time (same source as CustomerId), used for SLA threshold selection.</summary>
    public string DeliveryType { get; set; } = "Standard";

    /// <summary>Set once, the first time the SLA monitor detects this shipment has exceeded its delivery-type threshold. Null while on-time or already delivered/cancelled.</summary>
    public DateTime? SlaBreachedAt { get; set; }

    public Address PickupAddress { get; set; } = default!;
    public Address DeliveryAddress { get; set; } = default!;

    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ShipmentStatusHistory> StatusHistory { get; set; } = new();
}