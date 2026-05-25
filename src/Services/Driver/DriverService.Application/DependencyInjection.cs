using DriverService.Application.Abstractions;
using DriverService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DriverService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDriverService, Services.DriverService>();
        return services;
    }
}
