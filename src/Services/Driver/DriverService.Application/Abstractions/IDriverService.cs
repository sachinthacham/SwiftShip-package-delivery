using BuildingBlocks.Common;
using DriverService.Application.DTOs;

namespace DriverService.Application.Abstractions;

public interface IDriverService
{
    Task<DriverResponse> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedList<DriverResponse>> GetPagedAsync(int pageNumber, int pageSize, bool? isAvailable, CancellationToken cancellationToken = default);
    Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DriverResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> SetAvailabilityAsync(Guid id, SetDriverAvailabilityRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateLocationAsync(Guid id, UpdateDriverLocationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NearbyDriverResponse>> GetAvailableNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default);
}
