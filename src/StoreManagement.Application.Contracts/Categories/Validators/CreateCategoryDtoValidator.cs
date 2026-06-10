using FluentValidation;

namespace StoreManagement.Categories.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(input => input.Name)
            .NotEmpty()
            .WithMessage("Category name is required.")
            .MaximumLength(CategoryConsts.MaxNameLength)
            .WithMessage($"Category name cannot exceed {CategoryConsts.MaxNameLength} characters.");

        RuleFor(input => input.Description)
            .MaximumLength(CategoryConsts.MaxDescriptionLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Description))
            .WithMessage($"Category description cannot exceed {CategoryConsts.MaxDescriptionLength} characters.");

        RuleFor(input => input.SizeType)
            .IsInEnum()
            .WithMessage("Invalid category size type.");
    }
}