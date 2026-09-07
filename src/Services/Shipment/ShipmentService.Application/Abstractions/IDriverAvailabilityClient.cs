namespace ShipmentService.Application.Abstractions;

public record NearbyDriverResult(Guid DriverId, string Name, string VehicleType, double DistanceKm);

public interface IDriverAvailabilityClient
{
    /// <summary>Returns available drivers near the given point, nearest first. Empty if none are available or the lookup fails.</summary>
    Task<IReadOnlyList<NearbyDriverResult>> GetAvailableNearbyAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default);
}
