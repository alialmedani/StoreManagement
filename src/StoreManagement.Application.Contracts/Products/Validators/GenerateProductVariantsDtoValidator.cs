using FluentValidation;

namespace StoreManagement.Products.Validators;

public class GenerateProductVariantsDtoValidator : AbstractValidator<GenerateProductVariantsDto>
{
    public GenerateProductVariantsDtoValidator()
    {
        RuleFor(input => input.ProductId)
            .NotEmpty()
            .WithMessage("Product is required.");

        RuleFor(input => input.DefaultStockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Default stock quantity cannot be negative.");

        RuleForEach(input => input.Colors)
            .MaximumLength(ProductVariantConsts.MaxColorLength)
            .WithMessage($"Color cannot exceed {ProductVariantConsts.MaxColorLength} characters.");

        RuleForEach(input => input.Sizes)
            .MaximumLength(ProductVariantConsts.MaxSizeLength)
            .WithMessage($"Size cannot exceed {ProductVariantConsts.MaxSizeLength} characters.");
    }
}