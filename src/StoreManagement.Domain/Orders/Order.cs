using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;
namespace StoreManagement.Orders;

public class Order : FullAuditedAggregateRoot<Guid>
{
    public string OrderNumber { get; private set; } = string.Empty;

    public string CustomerName { get; private set; } = string.Empty;

    public string? CustomerPhone { get; private set; }

    public string? Note { get; private set; }

    public OrderStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    private readonly List<OrderItem> _items = new();

    public IReadOnlyCollection<OrderItem> Items => new ReadOnlyCollection<OrderItem>(_items);

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
        TotalAmount = 0;
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
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderProductVariantNotFound);
        }

        var existingItem = _items.FirstOrDefault(item =>
            item.ProductVariantId == productVariantId &&
            item.UnitPrice == unitPrice
        );

        if (existingItem != null)
        {
            existingItem.ChangeQuantity(existingItem.Quantity + quantity);
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
        int quantity,
        decimal unitPrice)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(item => item.Id == orderItemId);

        if (item == null)
        {
            throw new EntityNotFoundException(typeof(OrderItem), orderItemId);
        }

        item.ChangeQuantity(quantity);
        item.ChangeUnitPrice(unitPrice);

        RecalculateTotal();
    }

    public void RemoveItem(Guid orderItemId)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(item => item.Id == orderItemId);

        if (item == null)
        {
            throw new EntityNotFoundException(typeof(OrderItem), orderItemId);
        }

        _items.Remove(item);

        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderCannotBeConfirmed);
        }

        if (_items.Count == 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderItemRequired);
        }

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderCannotBeCancelled);
        }

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
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderCannotBeUpdated);
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.LineTotal);
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
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderTextTooLong)
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
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderTextTooLong)
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
    }
}