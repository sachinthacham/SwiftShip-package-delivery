using FluentValidation;
using TrackingService.Application.DTOs;

namespace TrackingService.Application.Validators;

public class AddTrackingRequestValidator : AbstractValidator<AddTrackingRequest>
{
    public AddTrackingRequestValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty();

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Status)
            .NotEmpty()
            .MaximumLength(100);
    }
}
