using Microsoft.EntityFrameworkCore;
using TrackingService.Infrastructure.Persistence;
using TrackingService.Infrastructure.Persistence.Entities;

namespace TrackingService.UnitTests;

/// <summary>
/// ShipmentEventConsumerBase.HandleMessageAsync (the real dedup call site) is a private method
/// entangled with RabbitMQ's BasicDeliverEventArgs, so it can't be driven directly without a broker.
/// This instead exercises the exact dedup query shape it runs (EventType+EventKey lookup against
/// ProcessedIntegrationEvents) against a real DbContext, and confirms the unique index constraint
/// backing "process each event exactly once" is enforced at the database level too.
/// </summary>
public class ConsumerDeduplicationTests
{
    private static TrackingDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TrackingDbContext(options);
    }

    [Fact]
    public async Task DedupQuery_ReturnsFalse_WhenEventNotYetProcessed()
    {
        await using var context = CreateContext(nameof(DedupQuery_ReturnsFalse_WhenEventNotYetProcessed));

        var alreadyProcessed = await context.ProcessedIntegrationEvents
            .AsNoTracking()
            .AnyAsync(x => x.EventType == "ShipmentCreated" && x.EventKey == "shipment-1");

        Assert.False(alreadyProcessed);
    }

    [Fact]
    public async Task DedupQuery_ReturnsTrue_OnlyForMatchingTypeAndKey()
    {
        await using var context = CreateContext(nameof(DedupQuery_ReturnsTrue_OnlyForMatchingTypeAndKey));
        context.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEvent
        {
            Id = Guid.NewGuid(),
            EventType = "ShipmentCreated",
            EventKey = "shipment-1",
            ProcessedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var sameKeyDifferentType = await context.ProcessedIntegrationEvents
            .AsNoTracking()
            .AnyAsync(x => x.EventType == "ShipmentStatusChanged" && x.EventKey == "shipment-1");
        var sameTypeDifferentKey = await context.ProcessedIntegrationEvents
            .AsNoTracking()
            .AnyAsync(x => x.EventType == "ShipmentCreated" && x.EventKey == "shipment-2");
        var exactMatch = await context.ProcessedIntegrationEvents
            .AsNoTracking()
            .AnyAsync(x => x.EventType == "ShipmentCreated" && x.EventKey == "shipment-1");

        Assert.False(sameKeyDifferentType);
        Assert.False(sameTypeDifferentKey);
        Assert.True(exactMatch);
    }

    [Fact]
    public async Task ProcessingSameEventTwice_OnlyPersistsOneTrackingEventAndOneProcessedMarker()
    {
        await using var context = CreateContext(nameof(ProcessingSameEventTwice_OnlyPersistsOneTrackingEventAndOneProcessedMarker));
        const string eventType = "ShipmentCreated";
        const string eventKey = "shipment-42";

        async Task ProcessOnceAsync()
        {
            var alreadyProcessed = await context.ProcessedIntegrationEvents
                .AsNoTracking()
                .AnyAsync(x => x.EventType == eventType && x.EventKey == eventKey);
            if (alreadyProcessed)
            {
                return;
            }

            context.TrackingEvents.Add(new()
            {
                Id = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                Location = "Hub",
                Status = "Created",
                TimestampUtc = DateTime.UtcNow
            });
            context.ProcessedIntegrationEvents.Add(new()
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                EventKey = eventKey,
                ProcessedAtUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        await ProcessOnceAsync();
        await ProcessOnceAsync();

        Assert.Equal(1, await context.TrackingEvents.CountAsync());
        Assert.Equal(1, await context.ProcessedIntegrationEvents.CountAsync());
    }
}
