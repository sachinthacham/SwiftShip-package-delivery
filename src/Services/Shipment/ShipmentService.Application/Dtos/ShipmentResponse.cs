namespace ShipmentService.Application.DTOs;

public record ShipmentResponse(
    Guid Id,
    Guid PackageId,
    Guid CustomerId,
    Guid? DriverId,
    string TrackingNumber,
    string Status,
    AddressDto PickupAddress,
    AddressDto DeliveryAddress,
    decimal Cost,
    string Currency,
    DateTime CreatedAt
);
