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
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<ShipmentRequestIdempotency> ShipmentRequestIdempotencies => Set<ShipmentRequestIdempotency>();
    public DbSet<OutboundIntegrationEvent> OutboundIntegrationEvents => Set<OutboundIntegrationEvent>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Rating> Ratings => Set<Rating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasIndex(s => s.TrackingNumber).IsUnique();
            entity.HasIndex(s => s.CustomerId);
            entity.HasIndex(s => s.DriverId);

            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(s => s.Cost).HasColumnType("decimal(10,2)");
            entity.Property(s => s.Currency).HasMaxLength(3).IsRequired();
            entity.Property(s => s.DeliveryType).HasMaxLength(20).IsRequired();

            entity.OwnsOne(s => s.PickupAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("PickupAddress_Street").IsRequired();
                address.Property(a => a.City).HasColumnName("PickupAddress_City").IsRequired();
                address.Property(a => a.State).HasColumnName("PickupAddress_State").IsRequired();
                address.Property(a => a.PostalCode).HasColumnName("PickupAddress_PostalCode").IsRequired();
                address.Property(a => a.Country).HasColumnName("PickupAddress_Country").IsRequired();
                address.Property(a => a.Latitude).HasColumnName("PickupAddress_Latitude");
                address.Property(a => a.Longitude).HasColumnName("PickupAddress_Longitude");
            });
            entity.Navigation(s => s.PickupAddress).IsRequired();

            entity.OwnsOne(s => s.DeliveryAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("DeliveryAddress_Street").IsRequired();
                address.Property(a => a.City).HasColumnName("DeliveryAddress_City").IsRequired();
                address.Property(a => a.State).HasColumnName("DeliveryAddress_State").IsRequired();
                address.Property(a => a.PostalCode).HasColumnName("DeliveryAddress_PostalCode").IsRequired();
                address.Property(a => a.Country).HasColumnName("DeliveryAddress_Country").IsRequired();
                address.Property(a => a.Latitude).HasColumnName("DeliveryAddress_Latitude");
                address.Property(a => a.Longitude).HasColumnName("DeliveryAddress_Longitude");
            });
            entity.Navigation(s => s.DeliveryAddress).IsRequired();
        });

        modelBuilder.Entity<ShipmentStatusHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<DeliveryAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FailureReason).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.ProofOfDeliveryUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
            entity.HasIndex(x => x.ShipmentId).IsUnique();
            entity.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.StripeSessionId).HasMaxLength(255);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ShipmentId).IsUnique();
            entity.Property(x => x.Comment).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
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