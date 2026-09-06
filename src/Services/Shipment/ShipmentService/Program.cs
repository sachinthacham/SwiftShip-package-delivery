using BuildingBlocks;
using FluentValidation;
using FluentValidation.AspNetCore;
using ShipmentService.API.BackgroundServices;
using ShipmentService.Application;
using ShipmentService.Application.DTOs;
using ShipmentService.API.Extensions;
using ShipmentService.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5003");
builder.AddSerilogLogging("Shipment Service");

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddServiceDefaults();
builder.Services.AddPackageDeliveryCors(builder.Configuration);
builder.Services.AddPackageDeliveryRateLimiting();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLocalFileStorage();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateShipmentRequest>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<SlaBreachMonitor>();

var app = builder.Build();

app.UsePackageDeliveryRequestLogging();
app.UseGlobalExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UsePackageDeliveryCors();
app.UsePackageDeliveryRateLimiting();
app.UseLocalFileStorage();

app.UseAuthentication();
app.UseAuthorization();
app.MapServiceDefaults("Shipment Service");
app.MapControllers();

app.Run();
