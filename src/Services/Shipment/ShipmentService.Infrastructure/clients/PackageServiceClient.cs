using PackageService.Contracts.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ShipmentService.Application.Abstractions;

namespace ShipmentService.Infrastructure.Clients;

public class PackageServiceClient : IPackageValidationClient
{
    private readonly PackageValidationGrpc.PackageValidationGrpcClient _packageValidationClient;
    private readonly ILogger<PackageServiceClient> _logger;
    private readonly ResiliencePipeline<bool> _resiliencePipeline;

    public PackageServiceClient(
        PackageValidationGrpc.PackageValidationGrpcClient packageValidationClient,
        ILogger<PackageServiceClient> logger)
    {
        _packageValidationClient = packageValidationClient;
        _logger = logger;

        var predicate = new PredicateBuilder<bool>()
            .Handle<RpcException>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>();

        _resiliencePipeline = new ResiliencePipelineBuilder<bool>()
            .AddRetry(new RetryStrategyOptions<bool>
            {
                ShouldHandle = predicate,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retrying Package gRPC validation. Attempt: {Attempt}",
                        args.AttemptNumber + 1);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                ShouldHandle = predicate,
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogWarning("Package gRPC circuit opened for {Duration}.", args.BreakDuration);
                    return default;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Package gRPC circuit closed.");
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    _logger.LogInformation("Package gRPC circuit half-open.");
                    return default;
                }
            })
            .Build();
    }

    public async Task<bool> PackageExists(Guid packageId, CancellationToken cancellationToken = default)
    {
        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var response = await _packageValidationClient.CheckPackageExistsAsync(
                new PackageExistsRequest { PackageId = packageId.ToString() },
                cancellationToken: ct);

            return response.Exists;
        }, cancellationToken);
    }
}
