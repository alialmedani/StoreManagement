using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StoreManagement.Common;

namespace StoreManagement.Orders;

public class OrderPaymentPagedRequestDto :
    StoreManagementPagedAndSortedResultRequestDto,
    IValidatableObject
{
    public PaymentMethod? PaymentMethod { get; set; }

    public DateTime? FromDate { get; set; }

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
                new[] { nameof(FromDate) }
            );
        }
    }
}