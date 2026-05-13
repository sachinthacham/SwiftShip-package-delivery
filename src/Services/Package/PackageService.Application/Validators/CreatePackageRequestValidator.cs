using FluentValidation;
using PackageService.Application.DTOs;

namespace PackageService.Application.Validators;

public class CreatePackageRequestValidator : AbstractValidator<CreatePackageRequest>
{
    public CreatePackageRequestValidator()
    {
        RuleFor(x => x.ReceiverName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.ReceiverPhone)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.ReceiverAddress)
            .NotEmpty()
            .MaximumLength(400);

        RuleFor(x => x.Weight)
            .GreaterThan(0);

        RuleFor(x => x.Length)
            .GreaterThan(0);

        RuleFor(x => x.Width)
            .GreaterThan(0);

        RuleFor(x => x.Height)
            .GreaterThan(0);
    }
}
