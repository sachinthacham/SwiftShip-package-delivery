using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BuildingBlocks.IntegrationEvents;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShipmentService.API.Controllers;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.DTOs;
using ShipmentService.Domain.Enums;
using ShipmentService.Infrastructure.Persistence;

namespace ShipmentService.UnitTests;

// End-to-end HTTP test of the shipment lifecycle: create -> assign driver -> status
// transitions -> delivery attempt -> verify final state. The DB is EF Core InMemory and
// gRPC (PackageService)/RabbitMQ are replaced with in-memory fakes via ConfigureTestServices,
// so this needs no live SQL Server, RabbitMQ, or PackageService — only the HTTP pipeline,
// routing, authorization, and the real Application+Infrastructure wiring are under test.
// WebApplicationFactory<ShipmentsController> is used instead of <Program> because Program.cs
// uses top-level statements without a `public partial class Program` marker; any public type
// from the same assembly works equally well for locating the host.
public class ShipmentLifecycleIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<ShipmentsController> _factory;
    private readonly HttpClient _client;
    private readonly Guid _fakeSenderId = Guid.NewGuid();

    public ShipmentLifecycleIntegrationTests()
    {
        // Computed once per test (not inside the lambda below): AddDbContext's configure
        // action re-runs on every scope resolution, so a Guid generated inline there would
        // hand each HTTP request a brand-new empty database.
        var databaseName = $"shipment-lifecycle-tests-{Guid.NewGuid()}";

        _factory = new WebApplicationFactory<ShipmentsController>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ShipmentDbContext>>();
                services.AddDbContext<ShipmentDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));

                services.RemoveAll<IPackageValidationClient>();
                services.AddSingleton<IPackageValidationClient>(
                    new FakePackageValidationClient(_fakeSenderId, weight: 4m, deliveryType: "Standard"));

                services.RemoveAll<IShipmentEventPublisher>();
                services.AddSingleton<IShipmentEventPublisher, NoOpShipmentEventPublisher>();

                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });
            });
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");
    }

    [Fact]
    public async Task FullLifecycle_CreateAssignTransitionAttemptDeliver_ReachesDeliveredState()
    {
        var pickup = new AddressDto("1 Pickup St", "City", "State", "00000", "Country", 10, 20);
        var delivery = new AddressDto("1 Delivery St", "City", "State", "00000", "Country", 11, 21);
        var createRequest = new CreateShipmentRequest(Guid.NewGuid(), pickup, delivery);

        var createResponse = await _client.PostAsJsonAsync("/api/shipments", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();
        Assert.NotNull(created);
        Assert.Equal(_fakeSenderId, created!.CustomerId);
        Assert.Equal("Created", created.Status);

        var driverId = Guid.NewGuid();
        var assignResponse = await _client.PostAsJsonAsync($"/api/shipments/{created.Id}/assign", new AssignDriverRequest(driverId));
        assignResponse.EnsureSuccessStatusCode();
        var assigned = await assignResponse.Content.ReadFromJsonAsync<ShipmentResponse>();
        Assert.Equal(driverId, assigned!.DriverId);

        foreach (var status in new[] { ShipmentStatus.PickedUp, ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery })
        {
            var statusResponse = await _client.PatchAsJsonAsync($"/api/shipments/{created.Id}/status", new UpdateShipmentStatusRequest(status));
            statusResponse.EnsureSuccessStatusCode();
            var updated = await statusResponse.Content.ReadFromJsonAsync<ShipmentResponse>();
            Assert.Equal(status.ToString(), updated!.Status);
        }

        var attemptResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{created.Id}/attempts",
            new LogDeliveryAttemptRequest(Successful: true, FailureReason: null, Notes: "Left with recipient"));
        attemptResponse.EnsureSuccessStatusCode();
        var attempt = await attemptResponse.Content.ReadFromJsonAsync<DeliveryAttemptResponse>();
        Assert.True(attempt!.Successful);
        Assert.Equal("Delivered", attempt.ShipmentStatus);

        var finalResponse = await _client.GetAsync($"/api/shipments/{created.Id}");
        finalResponse.EnsureSuccessStatusCode();
        var final = await finalResponse.Content.ReadFromJsonAsync<ShipmentResponse>();
        Assert.Equal("Delivered", final!.Status);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}

file sealed class FakePackageValidationClient : IPackageValidationClient
{
    private readonly Guid _senderId;
    private readonly decimal _weight;
    private readonly string _deliveryType;

    public FakePackageValidationClient(Guid senderId, decimal weight, string deliveryType)
    {
        _senderId = senderId;
        _weight = weight;
        _deliveryType = deliveryType;
    }

    public Task<PackageValidationResult?> GetPackageAsync(Guid packageId, CancellationToken cancellationToken = default)
        => Task.FromResult<PackageValidationResult?>(new PackageValidationResult(_weight, _deliveryType, _senderId));
}

file sealed class NoOpShipmentEventPublisher : IShipmentEventPublisher
{
    public Task PublishShipmentCreatedAsync(ShipmentCreatedEvent shipmentCreatedEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishShipmentStatusChangedAsync(ShipmentStatusChangedEvent shipmentStatusChangedEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishShipmentSlaBreachedAsync(ShipmentSlaBreachedEvent shipmentSlaBreachedEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

file sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string RoleHeader = "X-Test-Role";
    public const string UserIdHeader = "X-Test-UserId";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers.TryGetValue(RoleHeader, out var roleValues) ? roleValues.ToString() : "Admin";
        var userId = Request.Headers.TryGetValue(UserIdHeader, out var idValues) ? idValues.ToString() : Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
