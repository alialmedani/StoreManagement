using FluentValidation;

namespace StoreManagement.Reports.Validators;

public class SalesSummaryRequestDtoValidator
    : AbstractValidator<SalesSummaryRequestDto>
{
    public SalesSummaryRequestDtoValidator()
    {
        RuleFor(input => input.FromDate)
            .NotEmpty()
            .WithMessage("FromDate is required.");

        RuleFor(input => input.ToDate)
            .NotEmpty()
            .WithMessage("ToDate is required.");

        RuleFor(input => input.ToDate)
            .GreaterThanOrEqualTo(input => input.FromDate)
            .WithMessage("ToDate cannot be earlier than FromDate.");

        RuleFor(input => input.GroupBy)
            .IsInEnum()
            .WithMessage("GroupBy is invalid.");

        RuleFor(input => input.Status)
            .IsInEnum()
            .When(input => input.Status.HasValue)
            .WithMessage("Status is invalid.");

        RuleFor(input => input.PaymentStatus)
            .IsInEnum()
            .When(input => input.PaymentStatus.HasValue)
            .WithMessage("PaymentStatus is invalid.");
    }
}
