using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Orders;

/// <summary>
/// Immutable payment transaction recorded against an order.
/// </summary>
public class OrderPayment : CreationAuditedEntity<Guid>
{
    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public DateTime PaymentDate { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public string? Note { get; private set; }

    protected OrderPayment()
    {
    }

    public OrderPayment(
        Guid id,
        Guid orderId,
        decimal amount,
        PaymentMethod paymentMethod,
        DateTime paymentDate,
        string? referenceNumber = null,
        string? note = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment Id cannot be empty.",
                nameof(id)
            );
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "OrderId cannot be empty.",
                nameof(orderId)
            );
        }

        if (amount <= 0)
        {
            throw new BusinessException(
                "StoreManagement:OrderPaymentAmountInvalid"
            );
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new BusinessException(
                "StoreManagement:OrderPaymentDecimalPlacesInvalid"
            );
        }

        if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethod))
        {
            throw new BusinessException(
                "StoreManagement:OrderPaymentMethodInvalid"
            );
        }

        Id = id;
        OrderId = orderId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentDate = paymentDate;

        ReferenceNumber = NormalizeOptionalText(
            referenceNumber,
            nameof(ReferenceNumber),
            OrderConsts.MaxPaymentReferenceNumberLength
        );

        Note = NormalizeOptionalText(
            note,
            nameof(Note),
            OrderConsts.MaxPaymentNoteLength
        );
    }

    private static string? NormalizeOptionalText(
        string? value,
        string propertyName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new BusinessException(
                    "StoreManagement:OrderPaymentTextTooLong"
                )
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
    }
}