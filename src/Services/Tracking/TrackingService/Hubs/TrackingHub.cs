using Microsoft.AspNetCore.SignalR;

namespace TrackingService.API.Hubs;

/// <summary>
/// Live tracking updates, grouped by customer-facing tracking number. Anonymous access is allowed
/// (mirrors the public tracking endpoint) since a guest with only the tracking number should be able
/// to watch a shipment's progress without logging in.
/// </summary>
public class TrackingHub : Hub
{
    public async Task SubscribeToTracking(string trackingNumber)
    {
        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(trackingNumber));
        }
    }

    public async Task UnsubscribeFromTracking(string trackingNumber)
    {
        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(trackingNumber));
        }
    }

    public static string GroupName(string trackingNumber) => $"tracking:{trackingNumber}";
}
