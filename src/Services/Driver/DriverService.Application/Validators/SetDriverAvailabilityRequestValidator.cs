using DriverService.Application.DTOs;
using FluentValidation;

namespace DriverService.Application.Validators;

public class SetDriverAvailabilityRequestValidator : AbstractValidator<SetDriverAvailabilityRequest>
{
    public SetDriverAvailabilityRequestValidator()
    {
        RuleFor(x => x).NotNull();
    }
}
