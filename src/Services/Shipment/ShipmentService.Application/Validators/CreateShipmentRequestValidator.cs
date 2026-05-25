using FluentValidation;
using ShipmentService.Application.DTOs;

namespace ShipmentService.Application.Validators;

public class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentRequestValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty();

        RuleFor(x => x.PickupAddress)
            .NotEmpty()
            .MaximumLength(400);

        RuleFor(x => x.DeliveryAddress)
            .NotEmpty()
            .MaximumLength(400);
    }
}
