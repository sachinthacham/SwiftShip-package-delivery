using FluentValidation;
using ShipmentService.Application.DTOs;

namespace ShipmentService.Application.Validators;

public class UpdateShipmentStatusRequestValidator : AbstractValidator<UpdateShipmentStatusRequest>
{
    public UpdateShipmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
