using Microsoft.EntityFrameworkCore;
using TrackingService.Domain.Abstractions;
using TrackingService.Domain.Entities;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.Infrastructure.Repositories;

public class TrackingRepository : ITrackingRepository
{
    private readonly TrackingDbContext _dbContext;

    public TrackingRepository(TrackingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.TrackingEvents.Add(trackingEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrackingEvent>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrackingEvents
            .AsNoTracking()
            .Where(x => x.PackageId == packageId)
            .OrderByDescending(x => x.TimestampUtc)
            .ToListAsync(cancellationToken);
    }
}
