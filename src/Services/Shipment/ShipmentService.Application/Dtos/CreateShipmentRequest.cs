namespace ShipmentService.Application.DTOs;

public record CreateShipmentRequest(
    Guid PackageId,
    string PickupAddress,
    string DeliveryAddress
);