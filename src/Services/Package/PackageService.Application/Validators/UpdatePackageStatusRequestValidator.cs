using FluentValidation;
using PackageService.Application.DTOs;

namespace PackageService.Application.Validators;

public class UpdatePackageStatusRequestValidator : AbstractValidator<UpdatePackageStatusRequest>
{
    public UpdatePackageStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
