namespace ShipmentService.Application.DTOs;

public record ShipmentResponse(
    Guid Id,
    Guid PackageId,
    string TrackingNumber,
    string Status,
    string PickupAddress,
    string DeliveryAddress,
    DateTime CreatedAt
);