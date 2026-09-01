namespace ShipmentService.Domain.Enums;

public enum ShipmentStatus
{
    Created,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    FailedDelivery,
    Returned,
    Cancelled
}
