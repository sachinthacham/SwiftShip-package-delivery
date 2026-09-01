namespace BuildingBlocks.IntegrationEvents;

public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    Guid PackageId,
    Guid CustomerId,
    string TrackingNumber,
    string PickupAddress,
    DateTime OccurredAtUtc);

public sealed record ShipmentStatusChangedEvent(
    Guid ShipmentId,
    Guid PackageId,
    Guid CustomerId,
    string TrackingNumber,
    string Status,
    string Location,
    DateTime OccurredAtUtc);

public sealed record ShipmentSlaBreachedEvent(
    Guid ShipmentId,
    Guid PackageId,
    Guid CustomerId,
    string TrackingNumber,
    double SlaThresholdHours,
    DateTime OccurredAtUtc);
