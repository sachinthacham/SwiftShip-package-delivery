using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrackingService.Domain.Abstractions;
using TrackingService.Infrastructure.Persistence;
using TrackingService.Infrastructure.Repositories;

namespace TrackingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TrackingDb")
            ?? "Server=localhost;Database=package-delivery-tracking;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<TrackingDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ITrackingRepository, TrackingRepository>();
        return services;
    }
}
