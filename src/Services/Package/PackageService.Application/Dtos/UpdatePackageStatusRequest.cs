using PackageService.Domain.Enums;

namespace PackageService.Application.DTOs;

public record UpdatePackageStatusRequest(PackageStatus Status);
