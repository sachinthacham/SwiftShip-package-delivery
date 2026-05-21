using ShipmentService.Domain.Entities;

namespace ShipmentService.Domain.Abstractions;

public interface IShipmentRepository
{
    Task AddAsync(Shipment shipment, ShipmentStatusHistory initialStatusHistory, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetShipmentIdByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<bool> TryReserveIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SetIdempotencyResultAsync(string idempotencyKey, Guid shipmentId, CancellationToken cancellationToken = default);
    Task ReleaseIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
