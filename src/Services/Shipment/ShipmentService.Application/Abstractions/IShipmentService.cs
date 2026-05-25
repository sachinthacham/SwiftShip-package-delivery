using ShipmentService.Application.DTOs;

namespace ShipmentService.Application.Abstractions;

public interface IShipmentService
{
    Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<ShipmentResponse?> GetByIdAsync(Guid id);
}
