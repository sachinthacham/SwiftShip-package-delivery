namespace PackageService.Application.DTOs;

public record PackageResponse(
    Guid Id,
    Guid SenderId,
    string ReceiverName,
    string ReceiverPhone,
    AddressDto ReceiverAddress,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height,
    decimal DeclaredValue,
    string DeliveryType,
    string Status,
    DateTime CreatedAt
);
