using FluentValidation;

namespace StoreManagement.Orders.Validators;

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(input => input.ProductVariantId)
            .NotEmpty()
            .WithMessage("Product variant is required.");

        RuleFor(input => input.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}