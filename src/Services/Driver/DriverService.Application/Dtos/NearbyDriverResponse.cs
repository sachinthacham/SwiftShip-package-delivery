namespace DriverService.Application.DTOs;

public record NearbyDriverResponse(
    Guid DriverId,
    Guid UserId,
    string Name,
    string VehicleType,
    double CurrentLatitude,
    double CurrentLongitude,
    double DistanceKm);
