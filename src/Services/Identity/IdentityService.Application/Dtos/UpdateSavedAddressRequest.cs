namespace IdentityService.Application.Dtos;

public record UpdateSavedAddressRequest(
    string Label,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    double? Latitude,
    double? Longitude
);
