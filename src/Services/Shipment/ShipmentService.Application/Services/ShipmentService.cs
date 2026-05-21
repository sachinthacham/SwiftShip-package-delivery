using ShipmentService.Application.DTOs;
using ShipmentService.Application.Abstractions;
using BuildingBlocks.IntegrationEvents;
using ShipmentService.Domain.Abstractions;
using ShipmentService.Domain.Entities;

namespace ShipmentService.Application.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipments;
    private readonly IPackageValidationClient _packageValidationClient;
    private readonly IShipmentEventPublisher _shipmentEventPublisher;

    public ShipmentService(
        IShipmentRepository shipments,
        IPackageValidationClient packageValidationClient,
        IShipmentEventPublisher shipmentEventPublisher)
    {
        _shipments = shipments;
        _packageValidationClient = packageValidationClient;
        _shipmentEventPublisher = shipmentEventPublisher;
    }

    public async Task<ShipmentResponse> CreateAsync(
        CreateShipmentRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingShipmentId = await _shipments.GetShipmentIdByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingShipmentId.HasValue)
            {
                var existingShipment = await _shipments.GetByIdAsync(existingShipmentId.Value, cancellationToken);
                if (existingShipment is not null)
                {
                    return Map(existingShipment);
                }
            }

            var reserved = await _shipments.TryReserveIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (!reserved)
            {
                existingShipmentId = await _shipments.GetShipmentIdByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
                if (existingShipmentId.HasValue)
                {
                    var existingShipment = await _shipments.GetByIdAsync(existingShipmentId.Value, cancellationToken);
                    if (existingShipment is not null)
                    {
                        return Map(existingShipment);
                    }
                }

                throw new InvalidOperationException("A request with the same Idempotency-Key is currently being processed.");
            }
        }

        try
        {
            var exists = await _packageValidationClient.PackageExists(request.PackageId);

            if (!exists)
                throw new KeyNotFoundException("Package not found");

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                PackageId = request.PackageId,
                TrackingNumber = GenerateTrackingNumber(),
                Status = "Created",
                PickupAddress = request.PickupAddress,
                DeliveryAddress = request.DeliveryAddress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var initialStatusHistory = new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                Status = "Created",
                Location = request.PickupAddress,
                Timestamp = DateTime.UtcNow
            };

            await _shipments.AddAsync(shipment, initialStatusHistory, cancellationToken);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _shipments.SetIdempotencyResultAsync(idempotencyKey, shipment.Id, cancellationToken);
            }

            await _shipmentEventPublisher.PublishShipmentCreatedAsync(
                new ShipmentCreatedEvent(
                    shipment.Id,
                    shipment.PackageId,
                    shipment.PickupAddress,
                    DateTime.UtcNow),
                cancellationToken);

            return Map(shipment);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _shipments.ReleaseIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            }

            throw;
        }
    }

    public async Task<ShipmentResponse?> GetByIdAsync(Guid id)
    {
        var shipment = await _shipments.GetByIdAsync(id);
        return shipment is null ? null : Map(shipment);
    }

    private static string GenerateTrackingNumber()
    {
        return $"TRK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6]}";
    }

    private static ShipmentResponse Map(Shipment s)
    {
        return new ShipmentResponse(
            s.Id,
            s.PackageId,
            s.TrackingNumber,
            s.Status,
            s.PickupAddress,
            s.DeliveryAddress,
            s.CreatedAt
        );
    }
}