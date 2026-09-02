using Microsoft.Extensions.DependencyInjection;
using ShipmentService.Application.Abstractions;
using ShipmentService.Application.Pricing;
using ShipmentService.Application.Services;

namespace ShipmentService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IShipmentService, Services.ShipmentService>();
        services.AddSingleton<IShipmentPricingCalculator, ShipmentPricingCalculator>();
        return services;
    }
}
