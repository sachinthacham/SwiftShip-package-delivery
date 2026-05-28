using Microsoft.EntityFrameworkCore;
using TrackingService.Domain.Entities;
using TrackingService.Infrastructure.Persistence.Entities;

namespace TrackingService.Infrastructure.Persistence;

public class TrackingDbContext : DbContext
{
    public TrackingDbContext(DbContextOptions<TrackingDbContext> options)
        : base(options) { }

    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Location).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.HasIndex(x => x.PackageId);
            entity.HasIndex(x => x.TimestampUtc);
        });

        modelBuilder.Entity<ProcessedIntegrationEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EventKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ProcessedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.EventType, x.EventKey }).IsUnique();
        });
    }
}
