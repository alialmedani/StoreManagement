using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Orders;

public class Order : FullAuditedAggregateRoot<Guid>
{
    public string OrderNumber { get; private set; } = string.Empty;

    public string CustomerName { get; private set; } = string.Empty;

    public string? CustomerPhone { get; private set; }

    public string? Note { get; private set; }

    public OrderStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public OrderPaymentStatus PaymentStatus { get; private set; }

    public decimal PaidAmount { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTime? CancellationTime { get; private set; }

    [NotMapped]
    public decimal RemainingAmount =>
        Status == OrderStatus.Cancelled
            ? 0m
            : TotalAmount > PaidAmount
                ? TotalAmount - PaidAmount
                : 0m;

    private readonly List<OrderItem> _items = new();

    public IReadOnlyCollection<OrderItem> Items =>
        new ReadOnlyCollection<OrderItem>(_items);

    private readonly List<OrderPayment> _payments = new();

    public IReadOnlyCollection<OrderPayment> Payments =>
        new ReadOnlyCollection<OrderPayment>(_payments);

    protected Order()
    {
    }

    public Order(
        Guid id,
        string orderNumber,
        string customerName,
        string? customerPhone = null,
        string? note = null)
        : base(id)
    {
        SetOrderNumber(orderNumber);
        SetCustomerName(customerName);
        SetCustomerPhone(customerPhone);
        SetNote(note);

        Status = OrderStatus.Draft;
        TotalAmount = 0m;

        PaymentStatus = OrderPaymentStatus.Unpaid;
        PaidAmount = 0m;
    }

    public void SetOrderNumber(string orderNumber)
    {
        OrderNumber = NormalizeRequiredText(
            orderNumber,
            nameof(OrderNumber),
            OrderConsts.MaxOrderNumberLength,
            StoreManagementDomainErrorCodes.OrderNumberRequired
        );
    }

    public void SetCustomerName(string customerName)
    {
        CustomerName = NormalizeRequiredText(
            customerName,
            nameof(CustomerName),
            OrderConsts.MaxCustomerNameLength,
            StoreManagementDomainErrorCodes.OrderCustomerNameRequired
        );
    }

    public void SetCustomerPhone(string? customerPhone)
    {
        CustomerPhone = NormalizeOptionalText(
            customerPhone,
            nameof(CustomerPhone),
            OrderConsts.MaxCustomerPhoneLength
        );
    }

    public void SetNote(string? note)
    {
        Note = NormalizeOptionalText(
            note,
            nameof(Note),
            OrderConsts.MaxNoteLength
        );
    }

    public void UpdateHeader(
        string customerName,
        string? customerPhone,
        string? note)
    {
        EnsureDraft();

        SetCustomerName(customerName);
        SetCustomerPhone(customerPhone);
        SetNote(note);
    }

    public void AddItem(
        Guid productVariantId,
        string productName,
        string color,
        string size,
        int quantity,
        decimal unitPrice)
    {
        EnsureDraft();

        if (productVariantId == Guid.Empty)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes.OrderProductVariantNotFound
            );
        }

        var existingItem = _items.FirstOrDefault(item =>
            item.ProductVariantId == productVariantId &&
            item.UnitPrice == unitPrice
        );

        if (existingItem != null)
        {
            existingItem.ChangeQuantity(
                existingItem.Quantity + quantity
            );
        }
        else
        {
            var item = new OrderItem(
                Guid.NewGuid(),
                Id,
                productVariantId,
                productName,
                color,
                size,
                quantity,
                unitPrice
            );

            _items.Add(item);
        }

        RecalculateTotal();
    }

    public void UpdateItem(
        Guid orderItemId,
        int quantity)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(item =>
            item.Id == orderItemId
        );

        if (item == null)
        {
            throw new EntityNotFoundException(
                typeof(OrderItem),
                orderItemId
            );
        }

        item.ChangeQuantity(quantity);

        RecalculateTotal();
    }

    public void RemoveItem(Guid orderItemId)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(item =>
            item.Id == orderItemId
        );

        if (item == null)
        {
            throw new EntityNotFoundException(
                typeof(OrderItem),
                orderItemId
            );
        }

        _items.Remove(item);

        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes.OrderCannotBeConfirmed
            );
        }

        if (_items.Count == 0)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes.OrderItemRequired
            );
        }

        Status = OrderStatus.Confirmed;
    }

    public OrderPayment RecordPayment(
        Guid paymentId,
        decimal amount,
        PaymentMethod paymentMethod,
        DateTime paymentDate,
        string? referenceNumber,
        string? note)
    {
        if (Status != OrderStatus.Confirmed)
        {
            throw new BusinessException(
                "StoreManagement:OrderPaymentRequiresConfirmedOrder"
            );
        }

        if (paymentId == Guid.Empty)
        {
            throw new BusinessException(
                "StoreManagement:OrderPaymentIdInvalid"
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

        if (amount > RemainingAmount)
        {
            throw new BusinessException(
                    "StoreManagement:OrderPaymentExceedsRemainingAmount"
                )
                .WithData("TotalAmount", TotalAmount)
                .WithData("PaidAmount", PaidAmount)
                .WithData("RemainingAmount", RemainingAmount)
                .WithData("PaymentAmount", amount);
        }

        var payment = new OrderPayment(
            paymentId,
            Id,
            amount,
            paymentMethod,
            paymentDate,
            referenceNumber,
            note
        );

        _payments.Add(payment);

        PaidAmount += amount;

        RecalculatePaymentStatus();

        return payment;
    }

    public void Cancel(
        string cancellationReason,
        DateTime cancellationTime)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes.OrderCannotBeCancelled
            );
        }

        if (PaidAmount > 0)
        {
            throw new BusinessException(
                "StoreManagement:PaidOrderCannotBeCancelled"
            );
        }

        CancellationReason = NormalizeRequiredText(
            cancellationReason,
            nameof(CancellationReason),
            OrderConsts.MaxNoteLength,
            StoreManagementDomainErrorCodes.OrderTextTooLong
        );

        CancellationTime = cancellationTime;
        Status = OrderStatus.Cancelled;
    }

    public bool IsDraft()
    {
        return Status == OrderStatus.Draft;
    }

    public bool IsConfirmed()
    {
        return Status == OrderStatus.Confirmed;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes.OrderCannotBeUpdated
            );
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.LineTotal);
    }

    private void RecalculatePaymentStatus()
    {
        if (PaidAmount <= 0)
        {
            PaymentStatus = OrderPaymentStatus.Unpaid;
            return;
        }

        PaymentStatus = PaidAmount >= TotalAmount
            ? OrderPaymentStatus.Paid
            : OrderPaymentStatus.PartiallyPaid;
    }

    private static string NormalizeRequiredText(
        string value,
        string propertyName,
        int maxLength,
        string requiredErrorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(requiredErrorCode)
                .WithData("PropertyName", propertyName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new BusinessException(
                    StoreManagementDomainErrorCodes.OrderTextTooLong
                )
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
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
                    StoreManagementDomainErrorCodes.OrderTextTooLong
                )
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
    }
}