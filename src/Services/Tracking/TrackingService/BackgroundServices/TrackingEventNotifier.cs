using Microsoft.AspNetCore.SignalR;
using TrackingService.API.Hubs;
using TrackingService.Application.Abstractions;
using TrackingService.Application.DTOs;
using TrackingService.Domain.Entities;

namespace TrackingService.API.BackgroundServices;

/// <summary>Broadcasts a new tracking event over SignalR and best-effort emails the customer. Shared by both integration event consumers.</summary>
public static class TrackingEventNotifier
{
    public static async Task NotifyAsync(
        IServiceProvider scopedProvider,
        TrackingEvent trackingEvent,
        Guid customerId,
        string emailSubject,
        string emailBody,
        string smsMessage,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(trackingEvent.TrackingNumber))
        {
            var hubContext = scopedProvider.GetRequiredService<IHubContext<TrackingHub>>();
            var response = new TrackingResponse(
                trackingEvent.Id,
                trackingEvent.PackageId,
                trackingEvent.ShipmentId,
                trackingEvent.TrackingNumber,
                trackingEvent.Location,
                trackingEvent.Status,
                trackingEvent.TimestampUtc);

            await hubContext.Clients
                .Group(TrackingHub.GroupName(trackingEvent.TrackingNumber))
                .SendAsync("TrackingUpdated", response, cancellationToken);
        }

        try
        {
            var identityClient = scopedProvider.GetRequiredService<IIdentityUserLookupClient>();
            var contact = await identityClient.GetUserContactAsync(customerId, cancellationToken);

            if (contact is not null)
            {
                var notificationService = scopedProvider.GetRequiredService<INotificationService>();
                await notificationService.SendEmailAsync(
                    contact.Email, $"{contact.FirstName} {contact.LastName}", emailSubject, emailBody, cancellationToken);

                if (!string.IsNullOrWhiteSpace(contact.PhoneNumber))
                {
                    var smsSender = scopedProvider.GetRequiredService<ISmsSender>();
                    await smsSender.SendSmsAsync(contact.PhoneNumber, smsMessage, cancellationToken);
                }
                else
                {
                    logger.LogInformation("Customer {CustomerId} has no phone number on file; skipping SMS notification.", customerId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify customer {CustomerId} for tracking number {TrackingNumber}.", customerId, trackingEvent.TrackingNumber);
        }
    }
}
