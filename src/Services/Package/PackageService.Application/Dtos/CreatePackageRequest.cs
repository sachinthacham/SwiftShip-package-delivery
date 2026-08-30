using PackageService.Domain.Enums;

namespace PackageService.Application.DTOs;

public record CreatePackageRequest(
    string ReceiverName,
    string ReceiverPhone,
    AddressDto ReceiverAddress,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height,
    decimal DeclaredValue,
    DeliveryType DeliveryType
);
