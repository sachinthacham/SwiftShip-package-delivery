namespace ShipmentService.Application.DTOs;

public record BulkShipmentResult(Guid PackageId, bool Success, ShipmentResponse? Shipment, string? Error);
