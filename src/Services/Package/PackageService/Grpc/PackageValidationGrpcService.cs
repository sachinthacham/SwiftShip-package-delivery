using Grpc.Core;
using PackageService.Application.Abstractions;
using PackageService.Contracts.Grpc;

namespace PackageService.API.Grpc;

public class PackageValidationGrpcService : PackageValidationGrpc.PackageValidationGrpcBase
{
    private readonly IPackageService _packageService;

    public PackageValidationGrpcService(IPackageService packageService)
    {
        _packageService = packageService;
    }

    public override async Task<PackageExistsReply> CheckPackageExists(PackageExistsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.PackageId, out var packageId))
        {
            return new PackageExistsReply { Exists = false };
        }

        var package = await _packageService.GetByIdAsync(packageId);
        return new PackageExistsReply { Exists = package is not null };
    }
}
