namespace PackageService.Application.DTOs;

public record CreatePackageRequest(
    
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverAddress,
    decimal Weight,
    decimal Length,
    decimal Width,
    decimal Height
);