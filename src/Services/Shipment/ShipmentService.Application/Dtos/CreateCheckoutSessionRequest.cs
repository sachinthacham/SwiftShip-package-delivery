namespace ShipmentService.Application.DTOs;

public record CreateCheckoutSessionRequest(string SuccessUrl, string CancelUrl);
