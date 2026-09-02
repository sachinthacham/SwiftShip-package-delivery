namespace ShipmentService.Application.DTOs;

public record CreateShipmentRequest(
    Guid PackageId,
    AddressDto PickupAddress,
    AddressDto DeliveryAddress
);
