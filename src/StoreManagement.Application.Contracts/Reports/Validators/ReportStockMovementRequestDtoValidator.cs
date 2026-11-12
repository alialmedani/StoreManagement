using FluentValidation;

namespace StoreManagement.Reports.Validators;

public class ReportStockMovementRequestDtoValidator
    : AbstractValidator<ReportStockMovementRequestDto>
{
    public ReportStockMovementRequestDtoValidator()
    {
        RuleFor(input => input.ToDate)
            .GreaterThanOrEqualTo(input => input.FromDate)
            .When(input => input.FromDate.HasValue && input.ToDate.HasValue)
            .WithMessage("ToDate cannot be earlier than FromDate.");

        RuleFor(input => input.MovementType)
            .IsInEnum()
            .When(input => input.MovementType.HasValue)
            .WithMessage("MovementType is invalid.");
    }
}
