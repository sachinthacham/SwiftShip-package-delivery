using FluentValidation;
using ShipmentService.Application.DTOs;

namespace ShipmentService.Application.Validators;

public class CreateCheckoutSessionRequestValidator : AbstractValidator<CreateCheckoutSessionRequest>
{
    public CreateCheckoutSessionRequestValidator()
    {
        RuleFor(x => x.SuccessUrl).NotEmpty().Must(BeAnAbsoluteUrl).WithMessage("SuccessUrl must be a valid absolute URL.");
        RuleFor(x => x.CancelUrl).NotEmpty().Must(BeAnAbsoluteUrl).WithMessage("CancelUrl must be a valid absolute URL.");
    }

    private static bool BeAnAbsoluteUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
