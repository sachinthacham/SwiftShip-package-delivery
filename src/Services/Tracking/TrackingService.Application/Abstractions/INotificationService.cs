namespace TrackingService.Application.Abstractions;

public interface INotificationService
{
    Task SendEmailAsync(string toAddress, string toName, string subject, string body, CancellationToken cancellationToken = default);
}
