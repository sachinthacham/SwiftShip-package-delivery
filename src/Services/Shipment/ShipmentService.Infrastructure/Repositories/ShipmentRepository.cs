using Microsoft.EntityFrameworkCore;
using ShipmentService.Domain.Abstractions;
using ShipmentService.Domain.Entities;
using ShipmentService.Domain.Enums;
using ShipmentService.Domain.Models;
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

    public async Task AddAsync(Shipment shipment, ShipmentStatusHistory initialStatusHistory, Invoice invoice, CancellationToken cancellationToken = default)
    {
        _dbContext.Shipments.Add(shipment);
        _dbContext.StatusHistories.Add(initialStatusHistory);
        _dbContext.Invoices.Add(invoice);
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

    public async Task<Shipment?> AddDeliveryAttemptAsync(
        Guid shipmentId,
        DeliveryAttempt attempt,
        ShipmentStatus newStatus,
        ShipmentStatusHistory historyEntry,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(x => x.Id == shipmentId, cancellationToken);

        if (shipment is null)
        {
            return null;
        }

        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTime.UtcNow;

        _dbContext.DeliveryAttempts.Add(attempt);
        _dbContext.StatusHistories.Add(historyEntry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return shipment;
    }

    public async Task<Shipment?> UpdateStatusAsync(
        Guid shipmentId,
        ShipmentStatus newStatus,
        ShipmentStatusHistory historyEntry,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(x => x.Id == shipmentId, cancellationToken);

        if (shipment is null)
        {
            return null;
        }

        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTime.UtcNow;

        _dbContext.StatusHistories.Add(historyEntry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return shipment;
    }

    public async Task<Shipment?> AssignDriverAsync(Guid shipmentId, Guid driverId, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(x => x.Id == shipmentId, cancellationToken);

        if (shipment is null)
        {
            return null;
        }

        shipment.DriverId = driverId;
        shipment.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return shipment;
    }

    public async Task<(IReadOnlyList<Shipment> Items, int TotalCount, int PageNumber, int PageSize)> GetPagedAsync(
        int pageNumber, int pageSize, ShipmentStatus? status, Guid? customerId, Guid? driverId,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _dbContext.Shipments.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        if (driverId.HasValue)
            query = query.Where(x => x.DriverId == driverId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, pageNumber, pageSize);
    }

    public Task<DeliveryAttempt?> GetDeliveryAttemptAsync(Guid shipmentId, Guid attemptId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DeliveryAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == attemptId && x.ShipmentId == shipmentId, cancellationToken);
    }

    public async Task SetDeliveryAttemptProofUrlAsync(Guid attemptId, string proofOfDeliveryUrl, CancellationToken cancellationToken = default)
    {
        var attempt = await _dbContext.DeliveryAttempts
            .FirstOrDefaultAsync(x => x.Id == attemptId, cancellationToken);

        if (attempt is null)
        {
            return;
        }

        attempt.ProofOfDeliveryUrl = proofOfDeliveryUrl;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Invoice?> GetInvoiceByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShipmentId == shipmentId, cancellationToken);
    }

    public async Task<Invoice?> UpdatePaymentStatusAsync(Guid shipmentId, PaymentStatus status, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.ShipmentId == shipmentId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        invoice.PaymentStatus = status;
        invoice.PaidAt = status == PaymentStatus.Paid ? DateTime.UtcNow : invoice.PaidAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task SetInvoiceStripeSessionIdAsync(Guid invoiceId, string stripeSessionId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return;
        }

        invoice.StripeSessionId = stripeSessionId;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Invoice?> MarkInvoicePaidAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        if (invoice.PaymentStatus == PaymentStatus.Paid)
        {
            return invoice;
        }

        invoice.PaymentStatus = PaymentStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task<IReadOnlyList<Shipment>> GetActiveForSlaCheckAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Shipments
            .AsNoTracking()
            .Where(x => x.Status != ShipmentStatus.Delivered
                && x.Status != ShipmentStatus.Cancelled
                && x.SlaBreachedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryMarkSlaBreachedAsync(Guid shipmentId, DateTime breachedAtUtc, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(x => x.Id == shipmentId, cancellationToken);

        if (shipment is null || shipment.SlaBreachedAt is not null)
        {
            return false;
        }

        shipment.SlaBreachedAt = breachedAtUtc;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ShipmentAnalyticsSummary> GetAnalyticsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var todayStartUtc = DateTime.UtcNow.Date;

        var totalShipments = await _dbContext.Shipments.CountAsync(cancellationToken);

        var statusCounts = await _dbContext.Shipments
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var deliveredToday = await _dbContext.StatusHistories
            .Where(x => x.Status == ShipmentStatus.Delivered && x.Timestamp >= todayStartUtc)
            .Select(x => x.ShipmentId)
            .Distinct()
            .CountAsync(cancellationToken);

        var failedAttemptsToday = await _dbContext.DeliveryAttempts
            .Where(x => !x.Successful && x.AttemptedAt >= todayStartUtc)
            .CountAsync(cancellationToken);

        var slaBreachedTotal = await _dbContext.Shipments
            .CountAsync(x => x.SlaBreachedAt != null, cancellationToken);

        var slaBreachedToday = await _dbContext.Shipments
            .CountAsync(x => x.SlaBreachedAt != null && x.SlaBreachedAt >= todayStartUtc, cancellationToken);

        return new ShipmentAnalyticsSummary(
            totalShipments,
            statusCounts.ToDictionary(x => x.Status.ToString(), x => x.Count),
            deliveredToday,
            failedAttemptsToday,
            slaBreachedTotal,
            slaBreachedToday);
    }

    public async Task<IReadOnlyList<DriverPerformanceSummary>> GetDriverPerformanceAsync(CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.Shipments
            .Where(x => x.DriverId != null)
            .GroupBy(x => x.DriverId!.Value)
            .Select(g => new
            {
                DriverId = g.Key,
                TotalAssigned = g.Count(),
                Delivered = g.Count(x => x.Status == ShipmentStatus.Delivered),
                Failed = g.Count(x => x.Status == ShipmentStatus.FailedDelivery),
                DeliveredOnTime = g.Count(x => x.Status == ShipmentStatus.Delivered && x.SlaBreachedAt == null)
            })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(x => new DriverPerformanceSummary(
                x.DriverId,
                x.TotalAssigned,
                x.Delivered,
                x.Failed,
                x.Delivered == 0 ? 0 : Math.Round((double)x.DeliveredOnTime / x.Delivered, 4)))
            .ToList();
    }

    public Task<Rating?> GetRatingAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Ratings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShipmentId == shipmentId, cancellationToken);
    }

    public async Task<Rating> AddRatingAsync(Rating rating, CancellationToken cancellationToken = default)
    {
        _dbContext.Ratings.Add(rating);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return rating;
    }
}
