using TrackingService.Application.Abstractions;
using TrackingService.Application.DTOs;
using TrackingService.Domain.Abstractions;
using TrackingService.Domain.Entities;

namespace TrackingService.Application.Services;

public class TrackingService : ITrackingService
{
    private readonly ITrackingRepository _trackingRepository;

    public TrackingService(ITrackingRepository trackingRepository)
    {
        _trackingRepository = trackingRepository;
    }

    public async Task<TrackingResponse> AddAsync(AddTrackingRequest request, CancellationToken cancellationToken = default)
    {
        var trackingEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            PackageId = request.PackageId,
            Location = request.Location,
            Status = request.Status,
            TimestampUtc = DateTime.UtcNow
        };

        await _trackingRepository.AddAsync(trackingEvent, cancellationToken);
        return Map(trackingEvent);
    }

    public async Task<IReadOnlyList<TrackingResponse>> GetByPackageIdAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var history = await _trackingRepository.GetByPackageIdAsync(packageId, cancellationToken);
        return history.Select(Map).ToList();
    }

    private static TrackingResponse Map(TrackingEvent trackingEvent)
    {
        return new TrackingResponse(
            trackingEvent.Id,
            trackingEvent.PackageId,
            trackingEvent.Location,
            trackingEvent.Status,
            trackingEvent.TimestampUtc);
    }
}
