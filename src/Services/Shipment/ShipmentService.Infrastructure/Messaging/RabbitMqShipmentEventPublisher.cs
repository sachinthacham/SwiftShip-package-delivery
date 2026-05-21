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
        return PublishAsync(shipmentCreatedEvent, cancellationToken);
    }

    private async Task PublishAsync(ShipmentCreatedEvent shipmentCreatedEvent, CancellationToken cancellationToken)
    {
        const string eventType = nameof(ShipmentCreatedEvent);
        var eventKey = shipmentCreatedEvent.ShipmentId.ToString();
        var payload = JsonSerializer.Serialize(shipmentCreatedEvent);

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

        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var host = _configuration["RabbitMq:Host"] ?? "localhost";
            var username = _configuration["RabbitMq:Username"] ?? "guest";
            var password = _configuration["RabbitMq:Password"] ?? "guest";
            var queue = _configuration["RabbitMq:Queues:ShipmentCreated"] ?? "shipment.created";

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
