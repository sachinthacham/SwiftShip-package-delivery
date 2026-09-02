namespace ShipmentService.Application.Abstractions;

public record PackageValidationResult(decimal Weight, string DeliveryType, Guid SenderId);

public interface IPackageValidationClient
{
    /// <summary>Returns package details if it exists, otherwise null.</summary>
    Task<PackageValidationResult?> GetPackageAsync(Guid packageId, CancellationToken cancellationToken = default);
}
