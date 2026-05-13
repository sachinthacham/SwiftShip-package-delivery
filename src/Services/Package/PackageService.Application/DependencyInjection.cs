using Microsoft.Extensions.DependencyInjection;
using PackageService.Application.Abstractions;
using PackageService.Application.Services;

namespace PackageService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPackageService, Services.PackageService>();
        return services;
    }
}
