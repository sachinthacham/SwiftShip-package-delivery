using Microsoft.EntityFrameworkCore;
using PackageService.Domain.Entities;

namespace PackageService.Infrastructure.Persistence;

public class PackageDbContext : DbContext
{
    public PackageDbContext(DbContextOptions<PackageDbContext> options)
        : base(options) { }

    public DbSet<Package> Packages => Set<Package>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.ReceiverName).IsRequired();
            entity.Property(p => p.ReceiverPhone).IsRequired();
            entity.Property(p => p.ReceiverAddress).IsRequired();

           entity.Property(p => p.Weight).HasColumnType("decimal(10,2)");

entity.Property(p => p.Length).HasColumnType("decimal(10,2)");
entity.Property(p => p.Width).HasColumnType("decimal(10,2)");
entity.Property(p => p.Height).HasColumnType("decimal(10,2)");
        });
    }
}