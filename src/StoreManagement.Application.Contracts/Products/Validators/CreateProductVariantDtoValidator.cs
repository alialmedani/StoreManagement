using FluentValidation;

namespace StoreManagement.Products.Validators;

public class CreateProductVariantDtoValidator : AbstractValidator<CreateProductVariantDto>
{
    public CreateProductVariantDtoValidator()
    {
        RuleFor(input => input.ProductId)
            .NotEmpty()
            .WithMessage("Product is required.");

        RuleFor(input => input.Color)
            .MaximumLength(ProductVariantConsts.MaxColorLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Color))
            .WithMessage($"Color cannot exceed {ProductVariantConsts.MaxColorLength} characters.");

        RuleFor(input => input.Size)
            .MaximumLength(ProductVariantConsts.MaxSizeLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Size))
            .WithMessage($"Size cannot exceed {ProductVariantConsts.MaxSizeLength} characters.");

        RuleFor(input => input.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative.");
    }
}