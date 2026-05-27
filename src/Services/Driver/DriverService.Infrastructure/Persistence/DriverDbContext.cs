using DriverService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Infrastructure.Persistence;

public class DriverDbContext : DbContext
{
    public DriverDbContext(DbContextOptions<DriverDbContext> options)
        : base(options) { }

    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.VehicleNumber).IsRequired();
            entity.HasIndex(x => x.VehicleNumber).IsUnique();
            entity.HasIndex(x => x.IsAvailable);
        });
    }
}
