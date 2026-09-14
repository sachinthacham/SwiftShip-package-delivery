using BuildingBlocks;
using FluentValidation;
using FluentValidation.AspNetCore;
using TrackingService.API.BackgroundServices;
using TrackingService.API.Hubs;
using TrackingService.Application;
using TrackingService.Application.DTOs;
using TrackingService.API.Extensions;
using TrackingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5004");
builder.AddSerilogLogging("Tracking Service");

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddServiceDefaults();
builder.Services.AddPackageDeliveryCors(builder.Configuration);
builder.Services.AddPackageDeliveryRateLimiting();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AddTrackingRequest>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddHostedService<ShipmentCreatedConsumer>();
builder.Services.AddHostedService<ShipmentStatusChangedConsumer>();

var app = builder.Build();

app.UsePackageDeliveryRequestLogging();
app.UseGlobalExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UsePackageDeliveryCors();
app.UsePackageDeliveryRateLimiting();

app.UseAuthentication();
app.UseAuthorization();
app.MapServiceDefaults("Tracking Service");
app.MapControllers();
app.MapHub<TrackingHub>("/hubs/tracking");

app.Run();

public partial class Program { }
