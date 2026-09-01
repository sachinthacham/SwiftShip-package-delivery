using DriverService.Domain.Enums;

namespace DriverService.Application.DTOs;

public record CreateDriverRequest(Guid UserId, string Name, string VehicleNumber, VehicleType VehicleType);
