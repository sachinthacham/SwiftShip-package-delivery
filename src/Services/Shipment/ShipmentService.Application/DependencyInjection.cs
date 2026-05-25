using Microsoft.Extensions.DependencyInjection;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.Services;

namespace ShipmentService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IShipmentService, Services.ShipmentService>();
        return services;
    }
}
