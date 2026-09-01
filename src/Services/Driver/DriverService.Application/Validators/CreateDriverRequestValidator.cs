using DriverService.Application.DTOs;
using FluentValidation;

namespace DriverService.Application.Validators;

public class CreateDriverRequestValidator : AbstractValidator<CreateDriverRequest>
{
    public CreateDriverRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.VehicleNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.VehicleType)
            .IsInEnum();
    }
}
