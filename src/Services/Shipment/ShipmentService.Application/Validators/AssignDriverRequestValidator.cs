using FluentValidation;
using ShipmentService.Application.DTOs;

namespace ShipmentService.Application.Validators;

public class AssignDriverRequestValidator : AbstractValidator<AssignDriverRequest>
{
    public AssignDriverRequestValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty();
    }
}
