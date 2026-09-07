using ShipmentService.Domain.Enums;

namespace ShipmentService.Application.DTOs;

public record UpdatePaymentStatusRequest(PaymentStatus Status);
