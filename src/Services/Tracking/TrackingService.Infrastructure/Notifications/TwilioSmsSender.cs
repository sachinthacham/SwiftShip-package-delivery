using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using TrackingService.Application.Abstractions;

namespace TrackingService.Infrastructure.Notifications;

public class TwilioSmsSender : ISmsSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(IConfiguration configuration, ILogger<TwilioSmsSender> logger)
    {
        _configuration = configuration;
        _logger = logger;

        TwilioClient.Init(_configuration["Twilio:AccountSid"], _configuration["Twilio:AuthToken"]);
    }

    public async Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromNumber = _configuration["Twilio:FromPhoneNumber"]!;

            await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(toPhoneNumber));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SMS to {ToPhoneNumber}.", toPhoneNumber);
        }
    }
}
