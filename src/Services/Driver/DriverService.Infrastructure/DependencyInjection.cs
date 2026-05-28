using DriverService.Domain.Abstractions;
using DriverService.Infrastructure.Persistence;
using DriverService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DriverService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DriverDb")
            ?? "Server=localhost;Database=package-delivery-driver;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<DriverDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IDriverRepository, DriverRepository>();
        return services;
    }
}
