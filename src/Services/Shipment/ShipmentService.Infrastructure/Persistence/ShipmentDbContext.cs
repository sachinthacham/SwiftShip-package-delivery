using Microsoft.EntityFrameworkCore;
using ShipmentService.Domain.Entities;
using ShipmentService.Infrastructure.Persistence.Entities;

namespace ShipmentService.Infrastructure.Persistence;

public class ShipmentDbContext : DbContext
{
    public ShipmentDbContext(DbContextOptions<ShipmentDbContext> options)
        : base(options) { }

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentStatusHistory> StatusHistories => Set<ShipmentStatusHistory>();
    public DbSet<ShipmentRequestIdempotency> ShipmentRequestIdempotencies => Set<ShipmentRequestIdempotency>();
    public DbSet<OutboundIntegrationEvent> OutboundIntegrationEvents => Set<OutboundIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasIndex(s => s.TrackingNumber).IsUnique();

            entity.Property(s => s.Status).IsRequired();
        });

        modelBuilder.Entity<ShipmentStatusHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ShipmentRequestIdempotency>(entity =>
        {
            entity.HasKey(x => x.IdempotencyKey);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.ShipmentId).IsUnique().HasFilter("[ShipmentId] IS NOT NULL");
        });

        modelBuilder.Entity<OutboundIntegrationEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EventKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.EventType, x.EventKey }).IsUnique();
        });
    }
}