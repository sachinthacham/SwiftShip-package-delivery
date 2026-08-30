namespace PackageService.Application.DTOs;

public record AddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    double? Latitude = null,
    double? Longitude = null
);
