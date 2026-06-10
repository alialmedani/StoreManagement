using FluentValidation;

namespace StoreManagement.Products.Validators;

public class CreateBulkProductVariantsDtoValidator : AbstractValidator<CreateBulkProductVariantsDto>
{
    public CreateBulkProductVariantsDtoValidator()
    {
        RuleFor(input => input.ProductId)
            .NotEmpty()
            .WithMessage("Product is required.");

        RuleFor(input => input.Variants)
            .NotNull()
            .WithMessage("Variants are required.")
            .NotEmpty()
            .WithMessage("At least one variant is required.");

        RuleForEach(input => input.Variants)
            .SetValidator(new CreateBulkProductVariantItemDtoValidator());
    }
}