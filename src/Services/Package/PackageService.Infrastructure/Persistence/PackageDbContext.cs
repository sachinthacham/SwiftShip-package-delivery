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

            entity.OwnsOne(p => p.ReceiverAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("ReceiverAddress_Street").IsRequired();
                address.Property(a => a.City).HasColumnName("ReceiverAddress_City").IsRequired();
                address.Property(a => a.State).HasColumnName("ReceiverAddress_State").IsRequired();
                address.Property(a => a.PostalCode).HasColumnName("ReceiverAddress_PostalCode").IsRequired();
                address.Property(a => a.Country).HasColumnName("ReceiverAddress_Country").IsRequired();
                address.Property(a => a.Latitude).HasColumnName("ReceiverAddress_Latitude");
                address.Property(a => a.Longitude).HasColumnName("ReceiverAddress_Longitude");
            });
            entity.Navigation(p => p.ReceiverAddress).IsRequired();

            entity.Property(p => p.Weight).HasColumnType("decimal(10,2)");

            entity.Property(p => p.Length).HasColumnType("decimal(10,2)");
            entity.Property(p => p.Width).HasColumnType("decimal(10,2)");
            entity.Property(p => p.Height).HasColumnType("decimal(10,2)");

            entity.Property(p => p.DeclaredValue).HasColumnType("decimal(10,2)");

            entity.Property(p => p.DeliveryType).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}