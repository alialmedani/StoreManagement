using FluentValidation;

namespace StoreManagement.Orders.Validators;

public class AddOrderItemDtoValidator : AbstractValidator<AddOrderItemDto>
{
    public AddOrderItemDtoValidator()
    {
        RuleFor(input => input.ProductVariantId)
            .NotEmpty()
            .WithMessage("Product variant is required.");

        RuleFor(input => input.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}