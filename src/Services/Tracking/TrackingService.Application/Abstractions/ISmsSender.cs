namespace TrackingService.Application.Abstractions;

public interface ISmsSender
{
    Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);
}
