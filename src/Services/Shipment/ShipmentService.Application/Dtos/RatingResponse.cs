namespace ShipmentService.Application.DTOs;

public record RatingResponse(Guid Id, Guid ShipmentId, Guid CustomerId, int Stars, string? Comment, DateTime CreatedAt);
