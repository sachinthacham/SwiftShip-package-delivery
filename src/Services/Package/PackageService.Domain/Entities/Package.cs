namespace PackageService.Domain.Entities;

public class Package
{
    public Guid Id { get; set; }

    public Guid SenderId { get; set; }   // From Identity Service

    public string ReceiverName { get; set; } = default!;
    public string ReceiverPhone { get; set; } = default!;
    public string ReceiverAddress { get; set; } = default!;

    public decimal Weight { get; set; }

    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }

    public string Status { get; set; } = "Created";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}