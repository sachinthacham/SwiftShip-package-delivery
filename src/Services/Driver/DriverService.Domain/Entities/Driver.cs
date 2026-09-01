using DriverService.Domain.Enums;

namespace DriverService.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }

    /// <summary>The Identity Service User.Id of the Courier account this driver profile belongs to.</summary>
    public Guid UserId { get; set; }

    public string Name { get; set; } = default!;
    public string VehicleNumber { get; set; } = default!;
    public VehicleType VehicleType { get; set; }
    public bool IsAvailable { get; set; } = true;

    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
