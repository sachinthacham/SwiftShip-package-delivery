using TrackingService.Application.DTOs;

namespace TrackingService.Application.Abstractions;

public interface ITrackingService
{
    Task<TrackingResponse> AddAsync(AddTrackingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrackingResponse>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken = default);
}
