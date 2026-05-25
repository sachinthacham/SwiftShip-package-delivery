using System.Text;
using System.Text.Json;
using BuildingBlocks.IntegrationEvents;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;
using TrackingService.Domain.Entities;
using TrackingService.Infrastructure.Persistence;
using TrackingService.Infrastructure.Persistence.Entities;

namespace TrackingService.API.BackgroundServices;

public sealed class ShipmentCreatedConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;
    private readonly ResiliencePipeline _connectionPipeline;
    private readonly ResiliencePipeline _messagePipeline;
    private IConnection? _connection;
    private IChannel? _channel;

    public ShipmentCreatedConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<ShipmentCreatedConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var predicate = new PredicateBuilder().Handle<Exception>();

        _connectionPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = predicate,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential
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
                    _logger.LogWarning("RabbitMQ consumer connection circuit opened for {Duration}.", args.BreakDuration);
                    return default;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("RabbitMQ consumer connection circuit closed.");
                    return default;
                }
            })
            .Build();

        _messagePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = predicate,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception, "Retrying ShipmentCreated message processing.");
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = predicate,
                FailureRatio = 0.5,
                MinimumThroughput = 6,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(20),
                OnOpened = args =>
                {
                    _logger.LogWarning("Message processing circuit opened for {Duration}.", args.BreakDuration);
                    return default;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Message processing circuit closed.");
                    return default;
                }
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queue = _configuration["RabbitMq:Queues:ShipmentCreated"] ?? "shipment.created";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connection is null || !_connection.IsOpen || _channel is null || !_channel.IsOpen)
                {
                    await _connectionPipeline.ExecuteAsync(
                        async ct => await CreateChannelAsync(queue, ct),
                        stoppingToken);
                }

                var consumer = new AsyncEventingBasicConsumer(_channel!);
                consumer.ReceivedAsync += async (_, eventArgs) =>
                {
                    await HandleMessageAsync(eventArgs, stoppingToken);
                };

                await _channel!.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                _logger.LogInformation("Tracking consumer started on queue: {Queue}", queue);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShipmentCreated consumer failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task CreateChannelAsync(string queue, CancellationToken cancellationToken)
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

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var evt = JsonSerializer.Deserialize<ShipmentCreatedEvent>(json);

            if (evt is null)
            {
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
            await _messagePipeline.ExecuteAsync(async ct =>
            {
                const string eventType = nameof(ShipmentCreatedEvent);
                var eventKey = evt.ShipmentId.ToString();

                var alreadyProcessed = await dbContext.ProcessedIntegrationEvents
                    .AsNoTracking()
                    .AnyAsync(x => x.EventType == eventType && x.EventKey == eventKey, ct);

                if (alreadyProcessed)
                {
                    _logger.LogInformation("Skipping duplicate ShipmentCreated event for shipment {ShipmentId}.", evt.ShipmentId);
                    return;
                }

                var trackingEvent = new TrackingEvent
                {
                    Id = Guid.NewGuid(),
                    PackageId = evt.PackageId,
                    Location = evt.PickupAddress,
                    Status = "Shipment Created",
                    TimestampUtc = DateTime.UtcNow
                };

                var processedEvent = new ProcessedIntegrationEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = eventType,
                    EventKey = eventKey,
                    ProcessedAtUtc = DateTime.UtcNow
                };

                dbContext.TrackingEvents.Add(trackingEvent);
                dbContext.ProcessedIntegrationEvents.Add(processedEvent);
                await dbContext.SaveChangesAsync(ct);
            }, cancellationToken);

            await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing ShipmentCreated event.");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        finally
        {
            base.Dispose();
        }
    }
}
