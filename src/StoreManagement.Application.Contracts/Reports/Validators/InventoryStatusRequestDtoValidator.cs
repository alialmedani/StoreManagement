using FluentValidation;

namespace StoreManagement.Reports.Validators;

public class InventoryStatusRequestDtoValidator
    : AbstractValidator<InventoryStatusRequestDto>
{
    public InventoryStatusRequestDtoValidator()
    {
        RuleFor(input => input.LowStockThreshold)
            .GreaterThanOrEqualTo(0)
            .WithMessage("LowStockThreshold cannot be negative.");
    }
}
