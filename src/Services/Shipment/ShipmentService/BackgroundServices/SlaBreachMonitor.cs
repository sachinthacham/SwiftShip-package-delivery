using BuildingBlocks.IntegrationEvents;
using ShipmentService.Application.Abstractions;
using ShipmentService.Domain.Abstractions;
using ShipmentService.Domain.Entities;

namespace ShipmentService.API.BackgroundServices;

/// <summary>
/// Periodically scans active shipments for SLA breaches (elapsed time since creation exceeding a
/// per-delivery-type threshold) and publishes ShipmentSlaBreachedEvent exactly once per shipment.
/// </summary>
public class SlaBreachMonitor : BackgroundService
{
    private const double DefaultSameDayThresholdHours = 8;
    private const double DefaultExpressThresholdHours = 48;
    private const double DefaultStandardThresholdHours = 120;

    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaBreachMonitor> _logger;

    public SlaBreachMonitor(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<SlaBreachMonitor> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<double?>("Sla:CheckIntervalMinutes") ?? 15;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        do
        {
            try
            {
                await CheckOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA breach check failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var shipments = scope.ServiceProvider.GetRequiredService<IShipmentRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IShipmentEventPublisher>();

        var active = await shipments.GetActiveForSlaCheckAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var shipment in active)
        {
            var thresholdHours = GetThresholdHours(shipment.DeliveryType);
            if (now - shipment.CreatedAt < TimeSpan.FromHours(thresholdHours))
            {
                continue;
            }

            var marked = await shipments.TryMarkSlaBreachedAsync(shipment.Id, now, cancellationToken);
            if (!marked)
            {
                continue;
            }

            _logger.LogWarning(
                "Shipment {ShipmentId} ({TrackingNumber}) breached its {DeliveryType} SLA of {ThresholdHours}h.",
                shipment.Id, shipment.TrackingNumber, shipment.DeliveryType, thresholdHours);

            await publisher.PublishShipmentSlaBreachedAsync(
                new ShipmentSlaBreachedEvent(
                    shipment.Id,
                    shipment.PackageId,
                    shipment.CustomerId,
                    shipment.TrackingNumber,
                    thresholdHours,
                    now),
                cancellationToken);
        }
    }

    private double GetThresholdHours(string deliveryType)
    {
        var defaultHours = deliveryType switch
        {
            "SameDay" => DefaultSameDayThresholdHours,
            "Express" => DefaultExpressThresholdHours,
            _ => DefaultStandardThresholdHours
        };

        return _configuration.GetValue<double?>($"Sla:ThresholdHours:{deliveryType}") ?? defaultHours;
    }
}
