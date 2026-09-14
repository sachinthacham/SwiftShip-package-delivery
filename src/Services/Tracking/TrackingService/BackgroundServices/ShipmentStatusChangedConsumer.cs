using BuildingBlocks.IntegrationEvents;
using TrackingService.Domain.Entities;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.API.BackgroundServices;

public sealed class ShipmentStatusChangedConsumer : ShipmentEventConsumerBase<ShipmentStatusChangedEvent>
{
    public ShipmentStatusChangedConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<ShipmentStatusChangedConsumer> logger)
        : base(configuration, scopeFactory, logger)
    {
    }

    protected override string QueueConfigKey => "RabbitMq:Queues:ShipmentStatusChanged";
    protected override string DefaultQueueName => "shipment.status-changed";
    protected override string EventTypeName => nameof(ShipmentStatusChangedEvent);
    protected override string GetEventKey(ShipmentStatusChangedEvent evt) => $"{evt.ShipmentId}:{evt.Status}";

    protected override async Task<TrackingEvent> HandleEventAsync(
        ShipmentStatusChangedEvent evt, IServiceProvider scopedProvider, TrackingDbContext dbContext, CancellationToken cancellationToken)
    {
        var trackingEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            PackageId = evt.PackageId,
            ShipmentId = evt.ShipmentId,
            TrackingNumber = evt.TrackingNumber,
            Location = evt.Location,
            Status = evt.Status,
            TimestampUtc = DateTime.UtcNow
        };

        var logger = scopedProvider.GetRequiredService<ILogger<ShipmentStatusChangedConsumer>>();
        await TrackingEventNotifier.NotifyAsync(
            scopedProvider,
            trackingEvent,
            evt.CustomerId,
            emailSubject: $"Shipment {evt.TrackingNumber}: {evt.Status}",
            emailBody: $"Your shipment {evt.TrackingNumber} is now {evt.Status} ({evt.Location}).",
            smsMessage: $"Shipment {evt.TrackingNumber} is now {evt.Status}.",
            logger,
            cancellationToken);

        return trackingEvent;
    }
}
