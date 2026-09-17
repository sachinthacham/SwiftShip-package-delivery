using System.Net;
using System.Net.Http.Json;
using IdentityService.Application.Dtos;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.UnitTests;

public class AuthFlowIntegrationTests : IClassFixture<AuthFlowIntegrationTests.IdentityApiFactory>
{
    private readonly HttpClient _client;

    public AuthFlowIntegrationTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterThenLoginThenMe_ReturnsRegisteredUser()
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        var registerRequest = new RegisterRequest(email, "Password1!", "Integration", "Test");

        var registerResponse = await _client.PostAsJsonAsync("api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, "Password1!"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrEmpty(auth!.AccessToken));

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        meRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var meResponse = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Equal(email, me!.Email);
        Assert.Equal("Integration", me.FirstName);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPassword()
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        await _client.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, "Password1!", "Integration", "Test"));

        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public class IdentityApiFactory : WebApplicationFactory<Program>
    {
        // A dedicated internal service provider, shared across every DbContext instance this
        // factory creates, is required for the EF Core InMemory provider to reliably persist
        // data across the separate DbContext instances used by each HTTP request/scope -
        // without it, different scopes can silently resolve isolated in-memory stores.
        private static readonly IServiceProvider InMemoryServiceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        private readonly string _databaseName = $"identity-tests-{Guid.NewGuid()}";

        static IdentityApiFactory()
        {
            // appsettings.json ships a placeholder Jwt:Key ("SET_VIA_ENVIRONMENT_VARIABLE") that is
            // too short for HS256 (needs >= 256 bits); real deployments override it via env var.
            // Program.cs reads Jwt:Key eagerly (before WebApplicationFactory's ConfigureWebHost
            // hooks run), so a WebHostBuilder-level config override arrives too late and produces
            // a token signed with one key but validated against another. An environment variable
            // set before the host is built is read by the same default config source Program.cs
            // itself relies on, so generation and validation stay consistent.
            Environment.SetEnvironmentVariable("Jwt__Key", "test-signing-key-at-least-32-bytes-long-for-hs256!!");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<IdentityDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName)
                        .UseInternalServiceProvider(InMemoryServiceProvider));
            });
        }
    }
}
