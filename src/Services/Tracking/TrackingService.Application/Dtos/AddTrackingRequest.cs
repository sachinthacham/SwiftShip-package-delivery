namespace TrackingService.Application.DTOs;

public record AddTrackingRequest(
    Guid PackageId,
    string Location,
    string Status);
