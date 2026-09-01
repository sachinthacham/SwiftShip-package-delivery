using DriverService.Application.DTOs;
using FluentValidation;

namespace DriverService.Application.Validators;

public class UpdateDriverLocationRequestValidator : AbstractValidator<UpdateDriverLocationRequest>
{
    public UpdateDriverLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
