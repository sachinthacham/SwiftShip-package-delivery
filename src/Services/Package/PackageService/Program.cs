using BuildingBlocks;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using PackageService.Application;
using PackageService.Application.DTOs;
using PackageService.API.Grpc;
using PackageService.API.Extensions;
using PackageService.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogLogging("Package Service");
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
    options.ListenAnyIP(5006, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddServiceDefaults();
builder.Services.AddPackageDeliveryCors(builder.Configuration);
builder.Services.AddPackageDeliveryRateLimiting();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePackageRequest>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();

var app = builder.Build();

app.UsePackageDeliveryRequestLogging();
app.UseGlobalExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UsePackageDeliveryCors();
app.UsePackageDeliveryRateLimiting();

app.UseAuthentication();
app.UseAuthorization();
app.MapServiceDefaults("Package Service");
app.MapControllers();
app.MapGrpcService<PackageValidationGrpcService>();

app.Run();

public partial class Program { }