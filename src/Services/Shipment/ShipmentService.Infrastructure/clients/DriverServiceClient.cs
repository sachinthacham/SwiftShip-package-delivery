using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShipmentService.Application.Abstractions;

namespace ShipmentService.Infrastructure.Clients;

public class DriverServiceClient : IDriverAvailabilityClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DriverServiceClient> _logger;

    public DriverServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<DriverServiceClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NearbyDriverResult>> GetAvailableNearbyAsync(
        double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["Services:DriverBaseUrl"] ?? "http://localhost:5005";
            var apiKey = _configuration["Internal:ApiKey"];

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}/api/internal/drivers/available?lat={latitude}&lng={longitude}&radiusKm={radiusKm}");

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("X-Internal-Api-Key", apiKey);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Driver availability lookup returned {StatusCode}.", response.StatusCode);
                return Array.Empty<NearbyDriverResult>();
            }

            var drivers = await response.Content.ReadFromJsonAsync<List<NearbyDriverResponse>>(cancellationToken);
            return drivers?
                .Select(d => new NearbyDriverResult(d.DriverId, d.Name, d.VehicleType, d.DistanceKm))
                .ToList()
                ?? new List<NearbyDriverResult>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Driver availability lookup failed.");
            return Array.Empty<NearbyDriverResult>();
        }
    }

    private record NearbyDriverResponse(
        Guid DriverId, Guid UserId, string Name, string VehicleType,
        double CurrentLatitude, double CurrentLongitude, double DistanceKm);
}
