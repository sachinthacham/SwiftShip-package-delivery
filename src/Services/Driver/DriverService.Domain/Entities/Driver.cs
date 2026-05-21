namespace DriverService.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string VehicleNumber { get; set; } = default!;
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
