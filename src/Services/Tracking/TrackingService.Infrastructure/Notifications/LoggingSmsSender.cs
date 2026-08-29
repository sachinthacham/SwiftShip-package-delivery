using Microsoft.Extensions.Logging;
using TrackingService.Application.Abstractions;

namespace TrackingService.Infrastructure.Notifications;

/// <summary>Dev/demo fallback used when no Twilio credentials are configured: logs instead of sending.</summary>
public class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[No Twilio configured] Would send SMS to {ToPhoneNumber}: {Message}",
            toPhoneNumber, message);
        return Task.CompletedTask;
    }
}
