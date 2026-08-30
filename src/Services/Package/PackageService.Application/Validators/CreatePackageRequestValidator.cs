using FluentValidation;
using PackageService.Application.DTOs;
using PackageService.Domain.Enums;

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
            .NotNull()
            .SetValidator(new AddressDtoValidator());

        RuleFor(x => x.Weight)
            .GreaterThan(0);

        RuleFor(x => x.Length)
            .GreaterThan(0);

        RuleFor(x => x.Width)
            .GreaterThan(0);

        RuleFor(x => x.Height)
            .GreaterThan(0);

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.DeliveryType)
            .IsInEnum();
    }
}

public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}
