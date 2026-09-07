using BuildingBlocks.IntegrationEvents;

namespace ShipmentService.Application.Abstractions;

public interface IShipmentEventPublisher
{
    Task PublishShipmentCreatedAsync(ShipmentCreatedEvent shipmentCreatedEvent, CancellationToken cancellationToken = default);
    Task PublishShipmentStatusChangedAsync(ShipmentStatusChangedEvent shipmentStatusChangedEvent, CancellationToken cancellationToken = default);
    Task PublishShipmentSlaBreachedAsync(ShipmentSlaBreachedEvent shipmentSlaBreachedEvent, CancellationToken cancellationToken = default);
}
