namespace PackageService.Domain.ValueObjects;

public class Address
{
    public string Street { get; set; } = default!;
    public string City { get; set; } = default!;
    public string State { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string Country { get; set; } = default!;

    /// <summary>Geocoded coordinates, populated once the address is resolved. Null until geocoding runs.</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
