using BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddServiceDefaults();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseGlobalExceptionHandling();

app.MapServiceDefaults("API Gateway");
app.MapReverseProxy();

app.Run();
