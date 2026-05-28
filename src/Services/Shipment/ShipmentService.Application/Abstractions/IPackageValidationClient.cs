namespace ShipmentService.Application.Abstractions;

public interface IPackageValidationClient
{
    Task<bool> PackageExists(Guid packageId, CancellationToken cancellationToken = default);
}
