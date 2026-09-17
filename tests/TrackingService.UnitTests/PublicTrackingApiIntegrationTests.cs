using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TrackingService.Application.DTOs;
using TrackingService.Domain.Entities;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.UnitTests;

public class PublicTrackingApiIntegrationTests : IClassFixture<PublicTrackingApiIntegrationTests.TrackingServiceFactory>
{
    private readonly TrackingServiceFactory _factory;

    public PublicTrackingApiIntegrationTests(TrackingServiceFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetByTrackingNumber_ReturnsSeededHistory_WithNoAuthHeader()
    {
        const string trackingNumber = "TRK-PUBLIC-1";
        var packageId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
            db.TrackingEvents.Add(new TrackingEvent
            {
                Id = Guid.NewGuid(),
                PackageId = packageId,
                TrackingNumber = trackingNumber,
                Location = "Distribution Hub",
                Status = "InTransit",
                TimestampUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tracking/public/{trackingNumber}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<List<TrackingResponse>>();
        Assert.NotNull(history);
        Assert.Single(history!);
        Assert.Equal(trackingNumber, history![0].TrackingNumber);
        Assert.Equal("InTransit", history[0].Status);
    }

    [Fact]
    public async Task GetByTrackingNumber_ReturnsNotFound_ForUnknownTrackingNumber()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tracking/public/TRK-DOES-NOT-EXIST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public class TrackingServiceFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"tracking-service-tests-{Guid.NewGuid()}";

        public TrackingServiceFactory()
        {
            Environment.SetEnvironmentVariable("Jwt__Key", "this-is-a-test-only-signing-key-32bytes+");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TrackingDbContext>));
                if (dbDescriptor is not null)
                {
                    services.Remove(dbDescriptor);
                }

                services.AddDbContext<TrackingDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                // The RabbitMQ-backed consumers try to connect to a broker on startup; there is
                // none in this test environment, so they're removed rather than left to retry
                // pointlessly for the lifetime of the test host.
                services.RemoveAll(typeof(IHostedService));
            });
        }
    }
}
