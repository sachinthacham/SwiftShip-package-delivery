using Microsoft.Extensions.DependencyInjection;
using TrackingService.Application.Abstractions;
using TrackingService.Application.Services;

namespace TrackingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITrackingService, Services.TrackingService>();
        return services;
    }
}
