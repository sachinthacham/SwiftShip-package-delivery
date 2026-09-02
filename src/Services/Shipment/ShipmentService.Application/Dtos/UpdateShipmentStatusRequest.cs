using ShipmentService.Domain.Enums;

namespace ShipmentService.Application.DTOs;

public record UpdateShipmentStatusRequest(ShipmentStatus Status);
