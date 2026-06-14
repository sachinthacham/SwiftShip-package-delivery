namespace TrackingService.Application.DTOs;

public record TrackingResponse(
    Guid Id,
    Guid PackageId,
    string Location,
    string Status,
    DateTime TimestampUtc);
