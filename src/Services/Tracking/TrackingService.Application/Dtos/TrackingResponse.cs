namespace TrackingService.Application.DTOs;

public record TrackingResponse(
    Guid Id,
    Guid PackageId,
    Guid? ShipmentId,
    string? TrackingNumber,
    string Location,
    string Status,
    DateTime TimestampUtc);
