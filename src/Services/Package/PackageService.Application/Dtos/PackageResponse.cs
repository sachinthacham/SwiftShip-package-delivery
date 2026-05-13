namespace PackageService.Application.DTOs;

public record PackageResponse(
    Guid Id,
    Guid SenderId,
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height,
    string Status,
    DateTime CreatedAt
);