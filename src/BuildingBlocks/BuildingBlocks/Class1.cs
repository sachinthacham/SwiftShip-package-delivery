using System.Threading.RateLimiting;
using BuildingBlocks.Exceptions;
using BuildingBlocks.FileStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BuildingBlocks;

public static class ServiceDefaultsExtensions
{
    public const string CorsPolicyName = "PackageDeliveryClients";

    public static IServiceCollection AddServiceDefaults(this IServiceCollection services)
    {
        services.AddHealthChecks();
        services.AddProblemDetails();
        return services;
    }

    /// <summary>Configures Serilog as the host's logging provider, enriched with the owning service's name.</summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}");
        });

        return builder;
    }

    public static WebApplication UsePackageDeliveryRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        return app;
    }

    /// <summary>Named CORS policy allowing the configured Angular origin(s); falls back to the local dev server.</summary>
    public static IServiceCollection AddPackageDeliveryCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:4200" };

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static WebApplication UsePackageDeliveryCors(this WebApplication app)
    {
        app.UseCors(CorsPolicyName);
        return app;
    }

    /// <summary>Global fixed-window rate limit, partitioned by client IP.</summary>
    public static IServiceCollection AddPackageDeliveryRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    public static WebApplication UsePackageDeliveryRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }

    /// <summary>Registers the local-disk file storage implementation. Call <see cref="UseLocalFileStorage"/> to serve saved files back out.</summary>
    public static IServiceCollection AddLocalFileStorage(this IServiceCollection services)
    {
        services.AddSingleton<IFileStorageService, LocalDiskFileStorageService>();
        return services;
    }

    /// <summary>Serves files saved via <see cref="LocalDiskFileStorageService"/> as static content under the configured public base URL.</summary>
    public static WebApplication UseLocalFileStorage(this WebApplication app)
    {
        var rootPath = app.Configuration["FileStorage:RootPath"] ?? "App_Data/uploads";
        var publicBaseUrl = (app.Configuration["FileStorage:PublicBaseUrl"] ?? "/uploads").TrimEnd('/');

        var fullRootPath = Path.IsPathRooted(rootPath) ? rootPath : Path.Combine(AppContext.BaseDirectory, rootPath);
        Directory.CreateDirectory(fullRootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(fullRootPath),
            RequestPath = publicBaseUrl
        });

        return app;
    }

    public static WebApplication UseGlobalExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                var statusCode = exception switch
                {
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    ConflictException => StatusCodes.Status409Conflict,
                    ArgumentException => StatusCodes.Status400BadRequest,
                    InvalidOperationException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };

                logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                var problem = new
                {
                    type = $"https://httpstatuses.com/{statusCode}",
                    title = statusCode == 500 ? "An unexpected error occurred." : "Request failed.",
                    status = statusCode,
                    detail = exception?.Message,
                    traceId = context.TraceIdentifier
                };

                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        return app;
    }

    public static WebApplication MapServiceDefaults(this WebApplication app, string serviceName)
    {
        app.MapGet("/", () => Results.Ok(new
        {
            Service = serviceName,
            Status = "Running",
            UtcTimestamp = DateTime.UtcNow
        }));

        app.MapHealthChecks("/health");
        return app;
    }
}
