using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShipmentService.Application.Abstractions;
using ShipmentService.Domain.Abstractions;
using ShipmentService.Infrastructure.Clients;
using ShipmentService.Infrastructure.Messaging;
using ShipmentService.Infrastructure.Persistence;
using ShipmentService.Infrastructure.Repositories;

namespace ShipmentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ShipmentDb")
            ?? "Server=localhost;Database=package-delivery-shipment;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<ShipmentDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddGrpcClient<PackageService.Contracts.Grpc.PackageValidationGrpc.PackageValidationGrpcClient>(options =>
        {
            options.Address = new Uri(configuration["Services:PackageGrpcUrl"] ?? "http://localhost:5002");
        });
        services.AddScoped<IPackageValidationClient, PackageServiceClient>();
        services.AddScoped<IShipmentEventPublisher, RabbitMqShipmentEventPublisher>();

        return services;
    }
}
