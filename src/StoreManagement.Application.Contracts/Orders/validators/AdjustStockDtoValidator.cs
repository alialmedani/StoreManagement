using FluentValidation;

namespace StoreManagement.Inventory.Validators;

public class AdjustStockDtoValidator : AbstractValidator<AdjustStockDto>
{
    public AdjustStockDtoValidator()
    {
        RuleFor(input => input.ProductVariantId)
            .NotEmpty()
            .WithMessage("Product variant is required.");

        RuleFor(input => input.MovementType)
            .IsInEnum()
            .WithMessage("Invalid stock movement type.");

        RuleFor(input => input.MovementType)
            .Must(BeAllowedManualMovementType)
            .WithMessage("Only Increase, Decrease, and Adjustment are allowed for manual stock adjustment.");

        When(input => input.MovementType == StockMovementType.Increase ||
                      input.MovementType == StockMovementType.Decrease, () =>
        {
            RuleFor(input => input.Quantity)
                .NotNull()
                .WithMessage("Quantity is required for increase and decrease movements.")
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");

            RuleFor(input => input.NewQuantity)
                .Null()
                .WithMessage("New quantity should be sent only for adjustment movements.");
        });

        When(input => input.MovementType == StockMovementType.Adjustment, () =>
        {
            RuleFor(input => input.NewQuantity)
                .NotNull()
                .WithMessage("New quantity is required for adjustment movements.")
                .GreaterThanOrEqualTo(0)
                .WithMessage("New quantity cannot be negative.");

            RuleFor(input => input.Quantity)
                .Null()
                .WithMessage("Quantity should be sent only for increase and decrease movements.");
        });

        RuleFor(input => input.Note)
            .MaximumLength(InventoryConsts.MaxNoteLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Note))
            .WithMessage($"Note cannot exceed {InventoryConsts.MaxNoteLength} characters.");
    }

    private static bool BeAllowedManualMovementType(StockMovementType movementType)
    {
        return movementType == StockMovementType.Increase ||
               movementType == StockMovementType.Decrease ||
               movementType == StockMovementType.Adjustment;
    }
}