namespace DriverService.Application.DTOs;

public record DriverResponse(
    Guid Id,
    string Name,
    string VehicleNumber,
    bool IsAvailable,
    DateTime CreatedAtUtc);
