using FluentValidation;

namespace StoreManagement.Products.Validators;

public class UpdateProductVariantDtoValidator : AbstractValidator<UpdateProductVariantDto>
{
    public UpdateProductVariantDtoValidator()
    {
        RuleFor(input => input.Color)
            .MaximumLength(ProductVariantConsts.MaxColorLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Color))
            .WithMessage($"Color cannot exceed {ProductVariantConsts.MaxColorLength} characters.");

        RuleFor(input => input.Size)
            .MaximumLength(ProductVariantConsts.MaxSizeLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Size))
            .WithMessage($"Size cannot exceed {ProductVariantConsts.MaxSizeLength} characters.");
    }
}