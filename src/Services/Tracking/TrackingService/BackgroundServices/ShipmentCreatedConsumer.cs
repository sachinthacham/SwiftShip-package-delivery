using BuildingBlocks.IntegrationEvents;
using TrackingService.Domain.Entities;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.API.BackgroundServices;

public sealed class ShipmentCreatedConsumer : ShipmentEventConsumerBase<ShipmentCreatedEvent>
{
    public ShipmentCreatedConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<ShipmentCreatedConsumer> logger)
        : base(configuration, scopeFactory, logger)
    {
    }

    protected override string QueueConfigKey => "RabbitMq:Queues:ShipmentCreated";
    protected override string DefaultQueueName => "shipment.created";
    protected override string EventTypeName => nameof(ShipmentCreatedEvent);
    protected override string GetEventKey(ShipmentCreatedEvent evt) => evt.ShipmentId.ToString();

    protected override async Task<TrackingEvent> HandleEventAsync(
        ShipmentCreatedEvent evt, IServiceProvider scopedProvider, TrackingDbContext dbContext, CancellationToken cancellationToken)
    {
        var trackingEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            PackageId = evt.PackageId,
            ShipmentId = evt.ShipmentId,
            TrackingNumber = evt.TrackingNumber,
            Location = evt.PickupAddress,
            Status = "Shipment Created",
            TimestampUtc = DateTime.UtcNow
        };

        var logger = scopedProvider.GetRequiredService<ILogger<ShipmentCreatedConsumer>>();
        await TrackingEventNotifier.NotifyAsync(
            scopedProvider,
            trackingEvent,
            evt.CustomerId,
            emailSubject: $"Your shipment {evt.TrackingNumber} has been created",
            emailBody: $"We've received your shipment. Track it anytime with tracking number {evt.TrackingNumber}.",
            smsMessage: $"Your shipment {evt.TrackingNumber} has been created and is being processed.",
            logger,
            cancellationToken);

        return trackingEvent;
    }
}
