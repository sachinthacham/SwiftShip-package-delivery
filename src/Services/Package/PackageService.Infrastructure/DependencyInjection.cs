using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PackageService.Domain.Abstractions;
using PackageService.Infrastructure.Persistence;
using PackageService.Infrastructure.Repositories;

namespace PackageService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PackageDb")
            ?? "Server=localhost;Database=package-delivery-package;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<PackageDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPackageRepository, PackageRepository>();

        return services;
    }
}
