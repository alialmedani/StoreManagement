using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Inventory;

public class AdjustStockDto : IValidatableObject
{
    public Guid ProductVariantId { get; set; }

    public StockMovementType MovementType { get; set; }

    /// <summary>
    /// Required for Increase and Decrease.
    /// Always send a positive value.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    /// <summary>
    /// Required only for Adjustment.
    ///
    /// It may be negative only when AllowNegativeStock is enabled.
    /// The domain service is responsible for enforcing that setting.
    /// </summary>
    public int? NewQuantity { get; set; }

    [MaxLength(InventoryConsts.MaxNoteLength)]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (ProductVariantId == Guid.Empty)
        {
            yield return new ValidationResult(
                "ProductVariantId is required.",
                new[] { nameof(ProductVariantId) }
            );
        }

        switch (MovementType)
        {
            case StockMovementType.Increase:
            case StockMovementType.Decrease:
            {
                if (!Quantity.HasValue || Quantity.Value <= 0)
                {
                    yield return new ValidationResult(
                        "Quantity is required and must be greater than zero for Increase or Decrease.",
                        new[] { nameof(Quantity) }
                    );
                }

                if (NewQuantity.HasValue)
                {
                    yield return new ValidationResult(
                        "NewQuantity must not be provided for Increase or Decrease.",
                        new[] { nameof(NewQuantity) }
                    );
                }

                break;
            }

            case StockMovementType.Adjustment:
            {
                if (!NewQuantity.HasValue)
                {
                    yield return new ValidationResult(
                        "NewQuantity is required for Adjustment.",
                        new[] { nameof(NewQuantity) }
                    );
                }

                if (Quantity.HasValue)
                {
                    yield return new ValidationResult(
                        "Quantity must not be provided for Adjustment.",
                        new[] { nameof(Quantity) }
                    );
                }

                break;
            }

            default:
            {
                yield return new ValidationResult(
                    "Manual stock adjustment only supports Increase, Decrease and Adjustment.",
                    new[] { nameof(MovementType) }
                );

                break;
            }
        }
    }
}