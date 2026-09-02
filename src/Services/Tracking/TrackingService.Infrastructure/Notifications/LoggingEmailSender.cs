using Microsoft.Extensions.Logging;
using TrackingService.Application.Abstractions;

namespace TrackingService.Infrastructure.Notifications;

/// <summary>Dev/demo fallback used when no SMTP server is configured: logs instead of sending.</summary>
public class LoggingEmailSender : INotificationService
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toAddress, string toName, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[No SMTP configured] Would send email to {ToName} <{ToAddress}>: {Subject}\n{Body}",
            toName, toAddress, subject, body);
        return Task.CompletedTask;
    }
}
