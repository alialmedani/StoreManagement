using FluentValidation;

namespace StoreManagement.Products.Validators;

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(input => input.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(ProductConsts.MaxNameLength)
            .WithMessage($"Product name cannot exceed {ProductConsts.MaxNameLength} characters.");

        RuleFor(input => input.Description)
            .MaximumLength(ProductConsts.MaxDescriptionLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Description))
            .WithMessage($"Product description cannot exceed {ProductConsts.MaxDescriptionLength} characters.");

        RuleFor(input => input.Price)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than zero.");

        RuleFor(input => input.CategoryId)
            .NotEmpty()
            .WithMessage("Category is required.");

        RuleFor(input => input.TargetAudience)
            .IsInEnum()
            .WithMessage("Invalid product target audience.");
    }
}