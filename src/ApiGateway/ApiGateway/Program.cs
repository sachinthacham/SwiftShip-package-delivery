using BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.AddSerilogLogging("API Gateway");

builder.Services.AddServiceDefaults();
builder.Services.AddPackageDeliveryCors(builder.Configuration);
builder.Services.AddPackageDeliveryRateLimiting();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UsePackageDeliveryRequestLogging();
app.UseGlobalExceptionHandling();

app.UsePackageDeliveryCors();
app.UsePackageDeliveryRateLimiting();

app.MapServiceDefaults("API Gateway");
app.MapReverseProxy();

app.Run();
