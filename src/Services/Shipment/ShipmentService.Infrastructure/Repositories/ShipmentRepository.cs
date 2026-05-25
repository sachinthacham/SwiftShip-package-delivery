using Microsoft.EntityFrameworkCore;
using ShipmentService.Domain.Abstractions;
using ShipmentService.Domain.Entities;
using ShipmentService.Infrastructure.Persistence;
using ShipmentService.Infrastructure.Persistence.Entities;

namespace ShipmentService.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ShipmentDbContext _dbContext;

    public ShipmentRepository(ShipmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Shipment shipment, ShipmentStatusHistory initialStatusHistory, CancellationToken cancellationToken = default)
    {
        _dbContext.Shipments.Add(shipment);
        _dbContext.StatusHistories.Add(initialStatusHistory);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Guid?> GetShipmentIdByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ShipmentRequestIdempotencies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        return record?.ShipmentId;
    }

    public async Task<bool> TryReserveIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var record = new ShipmentRequestIdempotency
        {
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ShipmentRequestIdempotencies.Add(record);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task SetIdempotencyResultAsync(string idempotencyKey, Guid shipmentId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ShipmentRequestIdempotencies
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.ShipmentId = shipmentId;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ShipmentRequestIdempotencies
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey && x.ShipmentId == null, cancellationToken);

        if (record is null)
        {
            return;
        }

        _dbContext.ShipmentRequestIdempotencies.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
