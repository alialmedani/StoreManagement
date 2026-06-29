using FluentValidation;

namespace StoreManagement.Orders.Validators;

public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderDtoValidator()
    {
        RuleFor(input => input.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .MaximumLength(OrderConsts.MaxCustomerNameLength)
            .WithMessage($"Customer name cannot exceed {OrderConsts.MaxCustomerNameLength} characters.");

        RuleFor(input => input.CustomerAddress)
            .NotEmpty()
            .WithMessage("Customer address is required.")
            .MaximumLength(OrderConsts.MaxCustomerAddressLength)
            .WithMessage($"Customer address cannot exceed {OrderConsts.MaxCustomerAddressLength} characters.");

        RuleFor(input => input.CustomerPhone)
            .NotEmpty()
            .WithMessage("Customer phone is required.")
            .MaximumLength(OrderConsts.MaxCustomerPhoneLength)
            .WithMessage($"Customer phone cannot exceed {OrderConsts.MaxCustomerPhoneLength} characters.");

        RuleFor(input => input.Note)
            .MaximumLength(OrderConsts.MaxNoteLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Note))
            .WithMessage($"Note cannot exceed {OrderConsts.MaxNoteLength} characters.");
    }
}