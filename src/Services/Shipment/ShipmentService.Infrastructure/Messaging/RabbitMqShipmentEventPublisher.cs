using System.Text;
using System.Text.Json;
using BuildingBlocks.IntegrationEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ShipmentService.Application.Abstractions;
using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;
using ShipmentService.Infrastructure.Persistence;
using ShipmentService.Infrastructure.Persistence.Entities;

namespace ShipmentService.Infrastructure.Messaging;

public sealed class RabbitMqShipmentEventPublisher : IShipmentEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqShipmentEventPublisher> _logger;
    private readonly ShipmentDbContext _dbContext;
    private readonly ResiliencePipeline _resiliencePipeline;

    public RabbitMqShipmentEventPublisher(
        IConfiguration configuration,
        ShipmentDbContext dbContext,
        ILogger<RabbitMqShipmentEventPublisher> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;

        var predicate = new PredicateBuilder()
            .Handle<Exception>();

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = predicate,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retrying RabbitMQ publish. Attempt: {Attempt}",
                        args.AttemptNumber + 1);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = predicate,
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogWarning("RabbitMQ publish circuit opened for {Duration}.", args.BreakDuration);
                    return default;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("RabbitMQ publish circuit closed.");
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    _logger.LogInformation("RabbitMQ publish circuit half-open.");
                    return default;
                }
            })
            .Build();
    }

    public Task PublishShipmentCreatedAsync(ShipmentCreatedEvent shipmentCreatedEvent, CancellationToken cancellationToken = default)
    {
        return PublishAsync(
            nameof(ShipmentCreatedEvent),
            shipmentCreatedEvent.ShipmentId.ToString(),
            shipmentCreatedEvent,
            queueConfigKey: "RabbitMq:Queues:ShipmentCreated",
            defaultQueueName: "shipment.created",
            cancellationToken);
    }

    public Task PublishShipmentStatusChangedAsync(ShipmentStatusChangedEvent shipmentStatusChangedEvent, CancellationToken cancellationToken = default)
    {
        // Keyed by shipment id + status so the same transition is never double-published, while distinct transitions for the same shipment each get their own outbox row.
        var eventKey = $"{shipmentStatusChangedEvent.ShipmentId}:{shipmentStatusChangedEvent.Status}";

        return PublishAsync(
            nameof(ShipmentStatusChangedEvent),
            eventKey,
            shipmentStatusChangedEvent,
            queueConfigKey: "RabbitMq:Queues:ShipmentStatusChanged",
            defaultQueueName: "shipment.status-changed",
            cancellationToken);
    }

    public Task PublishShipmentSlaBreachedAsync(ShipmentSlaBreachedEvent shipmentSlaBreachedEvent, CancellationToken cancellationToken = default)
    {
        return PublishAsync(
            nameof(ShipmentSlaBreachedEvent),
            shipmentSlaBreachedEvent.ShipmentId.ToString(),
            shipmentSlaBreachedEvent,
            queueConfigKey: "RabbitMq:Queues:ShipmentSlaBreached",
            defaultQueueName: "shipment.sla-breached",
            cancellationToken);
    }

    private async Task PublishAsync<TEvent>(
        string eventType,
        string eventKey,
        TEvent evt,
        string queueConfigKey,
        string defaultQueueName,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(evt);

        var outboundEvent = await _dbContext.OutboundIntegrationEvents
            .FirstOrDefaultAsync(
                x => x.EventType == eventType && x.EventKey == eventKey,
                cancellationToken);

        if (outboundEvent is null)
        {
            outboundEvent = new OutboundIntegrationEvent
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                EventKey = eventKey,
                Payload = payload,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.OutboundIntegrationEvents.Add(outboundEvent);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                outboundEvent = await _dbContext.OutboundIntegrationEvents
                    .FirstOrDefaultAsync(
                        x => x.EventType == eventType && x.EventKey == eventKey,
                        cancellationToken);
            }
        }

        if (outboundEvent is not null && outboundEvent.PublishedAtUtc.HasValue)
        {
            _logger.LogInformation(
                "Skipping publish for already published event {EventType}/{EventKey}.",
                eventType,
                eventKey);
            return;
        }

        var queue = _configuration[queueConfigKey] ?? defaultQueueName;

        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var host = _configuration["RabbitMq:Host"] ?? "localhost";
            var username = _configuration["RabbitMq:Username"] ?? "guest";
            var password = _configuration["RabbitMq:Password"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password
            };

            await using var connection = await factory.CreateConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            var body = Encoding.UTF8.GetBytes(payload);
            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);
        }, cancellationToken);

        if (outboundEvent is not null)
        {
            outboundEvent.PublishedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
