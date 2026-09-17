using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrackingService.Application.Abstractions;
using TrackingService.Domain.Abstractions;
using TrackingService.Infrastructure.Clients;
using TrackingService.Infrastructure.Notifications;
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

        services.AddHttpClient<IIdentityUserLookupClient, IdentityUserLookupClient>();

        if (!string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
        {
            services.AddScoped<INotificationService, MailKitEmailSender>();
        }
        else
        {
            services.AddScoped<INotificationService, LoggingEmailSender>();
        }

        if (!string.IsNullOrWhiteSpace(configuration["Twilio:AccountSid"]) && !string.IsNullOrWhiteSpace(configuration["Twilio:AuthToken"]))
        {
            services.AddScoped<ISmsSender, TwilioSmsSender>();
        }
        else
        {
            services.AddScoped<ISmsSender, LoggingSmsSender>();
        }

        return services;
    }
}
