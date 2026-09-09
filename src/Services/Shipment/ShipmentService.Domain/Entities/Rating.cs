namespace ShipmentService.Domain.Entities;

/// <summary>One rating per shipment, left by the owning customer after delivery.</summary>
public class Rating
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid CustomerId { get; set; }

    public int Stars { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
