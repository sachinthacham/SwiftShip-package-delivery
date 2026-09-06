namespace IdentityService.Application.Dtos;

public record CreateSavedAddressRequest(
    string Label,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    double? Latitude,
    double? Longitude,
    bool IsDefault
);
