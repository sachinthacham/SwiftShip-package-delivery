using TrackingService.Domain.Entities;

namespace TrackingService.Domain.Abstractions;

public interface ITrackingRepository
{
    Task AddAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrackingEvent>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken = default);
}
