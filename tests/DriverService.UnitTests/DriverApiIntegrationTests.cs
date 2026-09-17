using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DriverService.Application.DTOs;
using DriverService.Domain.Enums;
using DriverService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DriverService.UnitTests;

public class DriverApiIntegrationTests : IClassFixture<DriverApiIntegrationTests.DriverApiFactory>
{
    private const string JwtKey = "test-signing-key-at-least-32-bytes-long-for-hs256!!";
    private const string JwtIssuer = "IdentityService";
    private const string JwtAudience = "PackageDeliverySystem";

    private readonly DriverApiFactory _factory;
    private readonly HttpClient _client;

    public DriverApiIntegrationTests(DriverApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string CreateToken(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpClient AuthenticatedAs(string role)
    {
        var token = CreateToken(Guid.NewGuid(), role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client;
    }

    [Fact]
    public async Task CreateDriver_ThenUpdateAvailability_ThenGet_ReflectsNewState()
    {
        var client = AuthenticatedAs("Admin");
        var createRequest = new CreateDriverRequest(Guid.NewGuid(), "Integration Driver", $"VN-{Guid.NewGuid():N}"[..10], VehicleType.Car);

        var createResponse = await client.PostAsJsonAsync("api/drivers", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<DriverResponse>();
        Assert.NotNull(created);
        Assert.True(created!.IsAvailable);

        var availabilityResponse = await client.PutAsJsonAsync($"api/drivers/{created.Id}/availability", new SetDriverAvailabilityRequest(false));
        Assert.Equal(HttpStatusCode.NoContent, availabilityResponse.StatusCode);

        var getResponse = await client.GetAsync($"api/drivers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updated = await getResponse.Content.ReadFromJsonAsync<DriverResponse>();
        Assert.NotNull(updated);
        Assert.False(updated!.IsAvailable);
    }

    [Fact]
    public async Task CreateDriver_ReturnsUnauthorized_WithoutToken()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync(
            "api/drivers",
            new CreateDriverRequest(Guid.NewGuid(), "No Auth", "NA-0001", VehicleType.Bicycle));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public class DriverApiFactory : WebApplicationFactory<Program>
    {
        // A dedicated internal service provider, shared across every DbContext instance this
        // factory creates, is required for the EF Core InMemory provider to reliably persist
        // data across the separate DbContext instances used by each HTTP request/scope -
        // without it, different scopes can silently resolve isolated in-memory stores.
        private static readonly IServiceProvider InMemoryServiceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        private readonly string _databaseName = $"driver-tests-{Guid.NewGuid()}";

        static DriverApiFactory()
        {
            // appsettings.json ships a placeholder Jwt:Key ("SET_VIA_ENVIRONMENT_VARIABLE") that is
            // too short for HS256 (needs >= 256 bits); real deployments override it via env var.
            // Program.cs reads Jwt:Key eagerly (before WebApplicationFactory's ConfigureWebHost
            // hooks run), so a WebHostBuilder-level config override arrives too late. An environment
            // variable set before the host is built is read by the same default config source
            // Program.cs itself relies on, keeping it consistent with the key used to mint test tokens.
            Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DriverDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<DriverDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName)
                        .UseInternalServiceProvider(InMemoryServiceProvider));
            });
        }
    }
}
