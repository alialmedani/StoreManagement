using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StoreManagement.Common;

namespace StoreManagement.Inventory;

public class StockMovementPagedRequestDto :
    StoreManagementPagedAndSortedResultRequestDto,
    IValidatableObject
{
    public Guid? CategoryId { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? ProductVariantId { get; set; }

    public StockMovementType? MovementType { get; set; }

    public StockMovementSourceType? SourceType { get; set; }

    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// Inclusive start date.
    /// Time part is ignored.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Inclusive end date.
    /// Time part is ignored.
    /// </summary>
    public DateTime? ToDate { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (FromDate.HasValue &&
            ToDate.HasValue &&
            FromDate.Value.Date > ToDate.Value.Date)
        {
            yield return new ValidationResult(
                "FromDate cannot be later than ToDate.",
                new[]
                {
                    nameof(FromDate),
                    nameof(ToDate)
                }
            );
        }
    }
}