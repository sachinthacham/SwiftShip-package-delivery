using BuildingBlocks;
using FluentValidation;
using FluentValidation.AspNetCore;
using TrackingService.API.BackgroundServices;
using TrackingService.Application;
using TrackingService.Application.DTOs;
using TrackingService.API.Extensions;
using TrackingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5004");

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AddTrackingRequest>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<ShipmentCreatedConsumer>();

var app = builder.Build();

app.UseGlobalExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapServiceDefaults("Tracking Service");
app.MapControllers();

app.Run();
