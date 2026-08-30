using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PackageService.Application.DTOs;
using PackageService.Domain.Enums;
using PackageService.Infrastructure.Persistence;

namespace PackageService.UnitTests;

public class PackagesApiIntegrationTests : IClassFixture<PackagesApiIntegrationTests.PackageServiceFactory>
{
    private readonly PackageServiceFactory _factory;

    public PackagesApiIntegrationTests(PackageServiceFactory factory)
    {
        _factory = factory;
    }

    private static readonly AddressDto TestAddress = new("1 Main St", "City", "State", "00000", "Country");

    private const string TestJwtKey = "this-is-a-test-only-signing-key-32bytes+";

    private static string BuildToken(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "IdentityService",
            audience: "PackageDeliverySystem",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpClient AuthenticatedClient(PackageServiceFactory factory, Guid userId, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BuildToken(userId, role));
        return client;
    }

    [Fact]
    public async Task CreateThenGetThenUpdateStatus_FullLifecycle_PersistsAcrossRequests()
    {
        var customerId = Guid.NewGuid();
        var customerClient = AuthenticatedClient(_factory, customerId, "Customer");

        var createRequest = new CreatePackageRequest(
            "Receiver", "555-0100", TestAddress, 5, 10, 10, 10, 100, DeliveryType.Standard);

        var createResponse = await customerClient.PostAsJsonAsync("/api/packages", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<PackageResponse>();
        Assert.NotNull(created);
        Assert.Equal("Created", created!.Status);
        Assert.Equal(customerId, created.SenderId);

        var getResponse = await customerClient.GetAsync($"/api/packages/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<PackageResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Created", fetched.Status);

        var dispatcherClient = AuthenticatedClient(_factory, Guid.NewGuid(), "Dispatcher");
        var updateResponse = await dispatcherClient.PatchAsJsonAsync(
            $"/api/packages/{created.Id}/status",
            new UpdatePackageStatusRequest(PackageStatus.PickedUp));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PackageResponse>();
        Assert.NotNull(updated);
        Assert.Equal("PickedUp", updated!.Status);

        var finalGet = await customerClient.GetAsync($"/api/packages/{created.Id}");
        var finalState = await finalGet.Content.ReadFromJsonAsync<PackageResponse>();
        Assert.Equal("PickedUp", finalState!.Status);
    }

    public class PackageServiceFactory : WebApplicationFactory<Program>
    {
        // Captured once per factory instance — the options delegate passed to AddDbContext
        // is re-invoked for every DI scope (i.e. every request), so generating the name
        // inside that delegate would hand each request its own empty database.
        private readonly string _databaseName = $"package-service-tests-{Guid.NewGuid()}";

        public PackageServiceFactory()
        {
            // Program.cs reads Jwt:Key synchronously while building WebApplicationBuilder,
            // before ConfigureWebHost/ConfigureAppConfiguration hooks are applied — so the
            // override has to land as an environment variable, which is read at that point.
            Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<PackageDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PackageDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
