using BuildingBlocks;
using FluentValidation;
using FluentValidation.AspNetCore;
using IdentityService.API.Extensions;
using IdentityService.Application;
using IdentityService.Application.Dtos;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5001");
builder.AddSerilogLogging("Identity Service");

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddServiceDefaults();
builder.Services.AddPackageDeliveryCors(builder.Configuration);
builder.Services.AddPackageDeliveryRateLimiting();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequest>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UsePackageDeliveryRequestLogging();
app.UseGlobalExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UsePackageDeliveryCors();
app.UsePackageDeliveryRateLimiting();

app.UseAuthentication();
app.UseAuthorization();
app.MapServiceDefaults("Identity Service");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await AdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; in integration tests can bootstrap this host.</summary>
public partial class Program { }