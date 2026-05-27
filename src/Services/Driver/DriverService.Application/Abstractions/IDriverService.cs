using DriverService.Application.DTOs;

namespace DriverService.Application.Abstractions;

public interface IDriverService
{
    Task<DriverResponse> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> SetAvailabilityAsync(Guid id, SetDriverAvailabilityRequest request, CancellationToken cancellationToken = default);
}
