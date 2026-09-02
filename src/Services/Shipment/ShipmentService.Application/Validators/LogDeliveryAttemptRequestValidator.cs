using FluentValidation;
using ShipmentService.Application.DTOs;
using ShipmentService.Domain.Enums;

namespace ShipmentService.Application.Validators;

public class LogDeliveryAttemptRequestValidator : AbstractValidator<LogDeliveryAttemptRequest>
{
    public LogDeliveryAttemptRequestValidator()
    {
        RuleFor(x => x.FailureReason)
            .NotEmpty()
            .Must(reason => Enum.TryParse<DeliveryAttemptFailureReason>(reason, out _))
            .WithMessage($"FailureReason must be one of: {string.Join(", ", Enum.GetNames<DeliveryAttemptFailureReason>())}.")
            .When(x => !x.Successful);

        RuleFor(x => x.FailureReason)
            .Empty()
            .WithMessage("FailureReason must not be set for a successful delivery attempt.")
            .When(x => x.Successful);

        RuleFor(x => x.Notes)
            .MaximumLength(500);
    }
}
