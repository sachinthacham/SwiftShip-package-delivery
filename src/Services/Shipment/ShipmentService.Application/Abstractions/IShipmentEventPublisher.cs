using BuildingBlocks.IntegrationEvents;

namespace ShipmentService.Application.Abstractions;

public interface IShipmentEventPublisher
{
    Task PublishShipmentCreatedAsync(ShipmentCreatedEvent shipmentCreatedEvent, CancellationToken cancellationToken = default);
}
