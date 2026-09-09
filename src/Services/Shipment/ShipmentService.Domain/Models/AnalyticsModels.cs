namespace ShipmentService.Domain.Models;

public record ShipmentAnalyticsSummary(
    int TotalShipments,
    IReadOnlyDictionary<string, int> CountsByStatus,
    int DeliveredToday,
    int FailedAttemptsToday,
    int SlaBreachedTotal,
    int SlaBreachedToday);

public record DriverPerformanceSummary(
    Guid DriverId,
    int TotalAssigned,
    int Delivered,
    int Failed,
    double OnTimeRate);
