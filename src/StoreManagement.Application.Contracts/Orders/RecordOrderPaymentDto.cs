using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Orders;

public class RecordOrderPaymentDto : IValidatableObject
{
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ErrorMessage = "Payment amount must be greater than zero."
    )]
    public decimal Amount { get; set; }

    [EnumDataType(
        typeof(PaymentMethod),
        ErrorMessage = "Payment method is not valid."
    )]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// When null, the backend uses the current server date and time.
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    [MaxLength(OrderConsts.MaxPaymentReferenceNumberLength)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(OrderConsts.MaxPaymentNoteLength)]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(PaymentMethod), PaymentMethod))
        {
            yield return new ValidationResult(
                "Payment method is not valid.",
                new[] { nameof(PaymentMethod) }
            );
        }

        if (Amount > 0 && decimal.Round(Amount, 2) != Amount)
        {
            yield return new ValidationResult(
                "Payment amount cannot contain more than two decimal places.",
                new[] { nameof(Amount) }
            );
        }
    }
}