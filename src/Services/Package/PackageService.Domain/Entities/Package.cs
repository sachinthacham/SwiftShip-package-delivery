using PackageService.Domain.Enums;
using PackageService.Domain.ValueObjects;

namespace PackageService.Domain.Entities;

public class Package
{
    public Guid Id { get; set; }

    public Guid SenderId { get; set; }   // From Identity Service

    public string ReceiverName { get; set; } = default!;
    public string ReceiverPhone { get; set; } = default!;
    public Address ReceiverAddress { get; set; } = default!;

    public decimal Weight { get; set; }

    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }

    public decimal DeclaredValue { get; set; }
    public DeliveryType DeliveryType { get; set; }

    public PackageStatus Status { get; set; } = PackageStatus.Created;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}