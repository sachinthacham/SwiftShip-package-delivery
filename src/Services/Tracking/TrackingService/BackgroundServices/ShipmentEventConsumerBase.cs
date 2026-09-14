using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TrackingService.Domain.Entities;
using TrackingService.Infrastructure.Persistence;
using TrackingService.Infrastructure.Persistence.Entities;

namespace TrackingService.API.BackgroundServices;

/// <summary>
/// Shared connection/retry/consume-loop and dedup/persist plumbing for a RabbitMQ-backed integration
/// event consumer. Subclasses supply the queue to listen on and how to turn one deserialized event into
/// the TrackingEvent to persist (plus any side effects, e.g. broadcasting or notifying).
/// </summary>
public abstract class ShipmentEventConsumerBase<TEvent> : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline _connectionPipeline;
    private readonly ResiliencePipeline _messagePipeline;
    private IConnection? _connection;
    private IChannel? _channel;

    protected abstract string QueueConfigKey { get; }
    protected abstract string DefaultQueueName { get; }
    protected abstract string EventTypeName { get; }
    protected abstract string GetEventKey(TEvent evt);

    /// <summary>Builds the TrackingEvent to persist for this event and performs any side effects (broadcast, notify). The caller adds the returned entity and saves.</summary>
    protected abstract Task<TrackingEvent> HandleEventAsync(
        TEvent evt, IServiceProvider scopedProvider, TrackingDbContext dbContext, CancellationToken cancellationToken);

    protected ShipmentEventConsumerBase(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger logger)
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
                    _logger.LogWarning(args.Outcome.Exception, "Retrying {EventType} message processing.", EventTypeName);
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
        var queue = _configuration[QueueConfigKey] ?? DefaultQueueName;

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
                _logger.LogInformation("{EventType} consumer started on queue: {Queue}", EventTypeName, queue);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{EventType} consumer failed. Retrying in 5 seconds.", EventTypeName);
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
            var evt = JsonSerializer.Deserialize<TEvent>(json);

            if (evt is null)
            {
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();

            await _messagePipeline.ExecuteAsync(async ct =>
            {
                var eventKey = GetEventKey(evt);

                var alreadyProcessed = await dbContext.ProcessedIntegrationEvents
                    .AsNoTracking()
                    .AnyAsync(x => x.EventType == EventTypeName && x.EventKey == eventKey, ct);

                if (alreadyProcessed)
                {
                    _logger.LogInformation("Skipping duplicate {EventType} event for key {EventKey}.", EventTypeName, eventKey);
                    return;
                }

                var trackingEvent = await HandleEventAsync(evt, scope.ServiceProvider, dbContext, ct);

                var processedEvent = new ProcessedIntegrationEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = EventTypeName,
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
            _logger.LogError(ex, "Failed processing {EventType} event.", EventTypeName);
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
