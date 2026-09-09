namespace ShipmentService.Application.DTOs;

public record ShipmentAnalyticsSummaryResponse(
    int TotalShipments,
    IReadOnlyDictionary<string, int> CountsByStatus,
    int DeliveredToday,
    int FailedAttemptsToday,
    int SlaBreachedTotal,
    int SlaBreachedToday);

public record DriverPerformanceResponse(
    Guid DriverId,
    int TotalAssigned,
    int Delivered,
    int Failed,
    double OnTimeRate);
