using FluentValidation;

namespace StoreManagement.Reports.Validators;

public class TopSellersRequestDtoValidator
    : AbstractValidator<TopSellersRequestDto>
{
    public TopSellersRequestDtoValidator()
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

        RuleFor(input => input.Top)
            .InclusiveBetween(1, 200)
            .WithMessage("Top must be between 1 and 200.");

        RuleFor(input => input.Direction)
            .IsInEnum()
            .WithMessage("Direction is invalid.");

        RuleFor(input => input.Status)
            .IsInEnum()
            .When(input => input.Status.HasValue)
            .WithMessage("Status is invalid.");
    }
}
