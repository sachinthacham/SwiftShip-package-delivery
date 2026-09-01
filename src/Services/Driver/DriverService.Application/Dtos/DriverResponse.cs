namespace DriverService.Application.DTOs;

public record DriverResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string VehicleNumber,
    string VehicleType,
    bool IsAvailable,
    double? CurrentLatitude,
    double? CurrentLongitude,
    DateTime CreatedAtUtc);
