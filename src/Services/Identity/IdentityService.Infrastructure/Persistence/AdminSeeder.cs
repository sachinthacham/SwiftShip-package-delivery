using BuildingBlocks.Authorization;
using IdentityService.Application.Abstractions;
using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Infrastructure.Persistence;

/// <summary>
/// Creates the first Admin account from configuration, since Admin/Dispatcher accounts can
/// otherwise only be created by an existing Admin (see AdminController). Only runs when
/// Identity:SeedAdmin:Email/Password are configured and no Admin exists yet.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["Identity:SeedAdmin:Email"];
        var password = configuration["Identity:SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var dbContext = services.GetRequiredService<IdentityDbContext>();
        if (await dbContext.Users.AnyAsync(u => u.Role == Roles.Admin))
            return;

        var hasher = services.GetRequiredService<IPasswordHasher>();

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hasher.Hash(password),
            FirstName = configuration["Identity:SeedAdmin:FirstName"] ?? "System",
            LastName = configuration["Identity:SeedAdmin:LastName"] ?? "Admin",
            Role = Roles.Admin,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
