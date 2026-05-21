namespace BuildingBlocks.IntegrationEvents;

public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    Guid PackageId,
    string PickupAddress,
    DateTime OccurredAtUtc);
