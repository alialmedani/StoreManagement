using FluentValidation;

namespace StoreManagement.Orders.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
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
            .MaximumLength(OrderConsts.MaxCustomerPhoneLength)
            .When(input => !string.IsNullOrWhiteSpace(input.CustomerPhone))
            .WithMessage($"Customer phone cannot exceed {OrderConsts.MaxCustomerPhoneLength} characters.");

        RuleFor(input => input.Note)
            .MaximumLength(OrderConsts.MaxNoteLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Note))
            .WithMessage($"Note cannot exceed {OrderConsts.MaxNoteLength} characters.");

        RuleFor(input => input.Items)
            .NotNull()
            .WithMessage("Order items are required.")
            .NotEmpty()
            .WithMessage("Order must contain at least one item.");

        RuleForEach(input => input.Items)
            .SetValidator(new CreateOrderItemDtoValidator());
    }
}