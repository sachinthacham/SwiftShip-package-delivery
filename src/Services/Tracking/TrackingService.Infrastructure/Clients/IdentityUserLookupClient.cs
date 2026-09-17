using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TrackingService.Application.Abstractions;

namespace TrackingService.Infrastructure.Clients;

public class IdentityUserLookupClient : IIdentityUserLookupClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityUserLookupClient> _logger;

    public IdentityUserLookupClient(HttpClient httpClient, IConfiguration configuration, ILogger<IdentityUserLookupClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<UserContact?> GetUserContactAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["Services:IdentityBaseUrl"] ?? "http://localhost:5001";
            var apiKey = _configuration["Internal:ApiKey"];

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/internal/users/{userId}");
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("X-Internal-Api-Key", apiKey);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Identity user lookup for {UserId} returned {StatusCode}.", userId, response.StatusCode);
                return null;
            }

            var contact = await response.Content.ReadFromJsonAsync<IdentityUserResponse>(cancellationToken);
            return contact is null
                ? null
                : new UserContact(contact.Id, contact.Email, contact.FirstName, contact.LastName, contact.PhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity user lookup for {UserId} failed.", userId);
            return null;
        }
    }

    private record IdentityUserResponse(Guid Id, string Email, string FirstName, string LastName, string Role, DateTime CreatedAt, string? PhoneNumber = null);
}
