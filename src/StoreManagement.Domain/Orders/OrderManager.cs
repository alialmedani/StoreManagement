using System;
using System.Linq;
using System.Threading.Tasks;
using StoreManagement.Inventory;
using StoreManagement.Products;
using StoreManagement.Settings;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Linq;
using Volo.Abp.Settings;

namespace StoreManagement.Orders;

public class OrderManager : DomainService
{
    private readonly IRepository<ProductVariant, Guid>
        _productVariantRepository;

    private readonly IRepository<OrderNumberSequence, Guid>
        _orderNumberSequenceRepository;

    private readonly InventoryManager _inventoryManager;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly ISettingProvider _settingProvider;

    public OrderManager(
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<OrderNumberSequence, Guid> orderNumberSequenceRepository,
        InventoryManager inventoryManager,
        IAsyncQueryableExecuter asyncExecuter,
        ISettingProvider settingProvider)
    {
        _productVariantRepository = productVariantRepository;
        _orderNumberSequenceRepository = orderNumberSequenceRepository;
        _inventoryManager = inventoryManager;
        _asyncExecuter = asyncExecuter;
        _settingProvider = settingProvider;
    }

    public async Task<Order> CreateAsync(
        string customerName,
        string customerAddress,
        string? customerPhone,
        string? note)
    {
        var orderNumberPrefix = await GetSettingValueAsync(
            StoreManagementSettings.OrderNumberPrefix,
            defaultValue: "ORD"
        );

        var orderNumber =
            await GenerateOrderNumberAsync(orderNumberPrefix);

        return new Order(
            GuidGenerator.Create(),
            orderNumber,
            customerName,
            customerAddress,
            customerPhone,
            note
        );
    }

    public async Task AddItemAsync(
        Order order,
        Guid productVariantId,
        int quantity)
    {
        var variantInfo =
            await GetAvailableProductVariantInfoAsync(
                productVariantId
            );

        var allowNegativeStock =
            await IsSettingEnabledAsync(
                StoreManagementSettings.AllowNegativeStock,
                defaultValue: false
            );

        long existingQuantity = order.Items
            .Where(item =>
                item.ProductVariantId == productVariantId)
            .Sum(item => (long)item.Quantity);

        var requiredQuantity =
            existingQuantity + quantity;

        if (!allowNegativeStock &&
            variantInfo.StockQuantity < requiredQuantity)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes
                    .OrderInsufficientStock
            );
        }

        order.AddItem(
            variantInfo.ProductVariantId,
            variantInfo.ProductName,
            variantInfo.Color,
            variantInfo.Size,
            quantity,
            variantInfo.Price
        );
    }

    public async Task UpdateItemAsync(
        Order order,
        Guid orderItemId,
        int quantity)
    {
        var orderItem = order.Items.FirstOrDefault(item =>
            item.Id == orderItemId
        );

        if (orderItem == null)
        {
            throw new EntityNotFoundException(
                typeof(OrderItem),
                orderItemId
            );
        }

        var variantInfo =
            await GetAvailableProductVariantInfoAsync(
                orderItem.ProductVariantId
            );

        var allowNegativeStock =
            await IsSettingEnabledAsync(
                StoreManagementSettings.AllowNegativeStock,
                defaultValue: false
            );

        long otherQuantityForSameVariant = order.Items
            .Where(item =>
                item.ProductVariantId ==
                orderItem.ProductVariantId &&
                item.Id != orderItemId)
            .Sum(item => (long)item.Quantity);

        var requiredQuantity =
            otherQuantityForSameVariant + quantity;

        if (!allowNegativeStock &&
            variantInfo.StockQuantity < requiredQuantity)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes
                    .OrderInsufficientStock
            );
        }

        order.UpdateItem(
            orderItemId,
            quantity
        );
    }

    public async Task ConfirmAsync(Order order)
    {
        var allowNegativeStock =
            await IsSettingEnabledAsync(
                StoreManagementSettings.AllowNegativeStock,
                defaultValue: false
            );

        var quantityByVariant = order.Items
            .GroupBy(item => item.ProductVariantId)
            .Select(group => new
            {
                ProductVariantId = group.Key,
                Quantity = group.Sum(item =>
                    (long)item.Quantity)
            })
            .ToList();

        foreach (var itemGroup in quantityByVariant)
        {
            var variantInfo =
                await GetAvailableProductVariantInfoAsync(
                    itemGroup.ProductVariantId
                );

            if (!allowNegativeStock &&
                variantInfo.StockQuantity <
                itemGroup.Quantity)
            {
                throw new BusinessException(
                    StoreManagementDomainErrorCodes
                        .OrderInsufficientStock
                );
            }
        }

        order.Confirm();

        foreach (var item in order.Items)
        {
            await _inventoryManager.RecordSaleAsync(
                order.Id,
                item.ProductVariantId,
                item.Quantity,
                $"Sale from order {order.OrderNumber}"
            );
        }
    }

    public OrderPayment RecordPayment(
        Order order,
        decimal amount,
        PaymentMethod paymentMethod,
        DateTime? paymentDate,
        string? referenceNumber,
        string? note)
    {
        var effectivePaymentDate =
            paymentDate ?? Clock.Now;

        return order.RecordPayment(
            GuidGenerator.Create(),
            amount,
            paymentMethod,
            effectivePaymentDate,
            referenceNumber,
            note
        );
    }

    public async Task CancelAsync(
        Order order,
        string cancellationReason)
    {
        var wasConfirmed = order.IsConfirmed();

        if (wasConfirmed)
        {
            var allowCancelConfirmedOrder =
                await IsSettingEnabledAsync(
                    StoreManagementSettings
                        .AllowCancelConfirmedOrder,
                    defaultValue: true
                );

            if (!allowCancelConfirmedOrder)
            {
                throw new BusinessException(
                    StoreManagementDomainErrorCodes
                        .OrderCannotBeCancelled
                );
            }
        }

        order.Cancel(
            cancellationReason,
            Clock.Now
        );

        if (!wasConfirmed)
        {
            return;
        }

        foreach (var item in order.Items)
        {
            await _inventoryManager
                .RecordOrderCancellationAsync(
                    order.Id,
                    item.ProductVariantId,
                    item.Quantity,
                    $"Cancellation of order {order.OrderNumber}"
                );
        }
    }

    private async Task<string> GenerateOrderNumberAsync(
        string prefix)
    {
        var safePrefix =
            NormalizeOrderNumberPrefix(prefix);

        var year = Clock.Now.Year;

        var query =
            await _orderNumberSequenceRepository
                .GetQueryableAsync();

        var sequence =
            await _asyncExecuter.FirstOrDefaultAsync(
                query.Where(sequence =>
                    sequence.Prefix == safePrefix &&
                    sequence.Year == year)
            );

        long number;

        if (sequence == null)
        {
            sequence = new OrderNumberSequence(
                GuidGenerator.Create(),
                safePrefix,
                year
            );

            number = sequence.GetNextNumber();

            await _orderNumberSequenceRepository.InsertAsync(
                sequence,
                autoSave: false
            );
        }
        else
        {
            number = sequence.GetNextNumber();

            await _orderNumberSequenceRepository.UpdateAsync(
                sequence,
                autoSave: false
            );
        }

        return
            $"{safePrefix}-{year}-{number.ToString().PadLeft(OrderConsts.OrderNumberSequenceLength, '0')}";
    }

    private static string NormalizeOrderNumberPrefix(
        string prefix)
    {
        var safePrefix =
            string.IsNullOrWhiteSpace(prefix)
                ? "ORD"
                : prefix.Trim().ToUpperInvariant();

        if (safePrefix.Length >
            OrderConsts.MaxOrderNumberPrefixLength)
        {
            throw new BusinessException(
                    StoreManagementDomainErrorCodes
                        .OrderTextTooLong
                )
                .WithData(
                    "PropertyName",
                    "OrderNumberPrefix"
                )
                .WithData(
                    "MaxLength",
                    OrderConsts.MaxOrderNumberPrefixLength
                );
        }

        return safePrefix;
    }

    private async Task<ProductVariantInfo>
        GetAvailableProductVariantInfoAsync(
            Guid productVariantId)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes
                    .OrderProductVariantNotFound
            );
        }

        var query =
            await _productVariantRepository
                .GetQueryableAsync();

        var variantInfo =
            await _asyncExecuter.FirstOrDefaultAsync(
                query
                    .Where(variant =>
                        variant.Id == productVariantId)
                    .Select(variant =>
                        new ProductVariantInfo(
                            variant.Id,
                            variant.Product.Name,
                            variant.Color,
                            variant.Size,
                            variant.StockQuantity,
                            variant.Product.Price,
                            variant.IsActive,
                            variant.Product.IsActive,
                            variant.Product.Category.IsActive
                        ))
            );

        if (variantInfo == null)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes
                    .OrderProductVariantNotFound
            );
        }

        if (!variantInfo.IsActive ||
            !variantInfo.ProductIsActive ||
            !variantInfo.CategoryIsActive)
        {
            throw new BusinessException(
                StoreManagementDomainErrorCodes
                    .OrderProductVariantInactive
            );
        }

        return variantInfo;
    }

    private async Task<bool> IsSettingEnabledAsync(
        string settingName,
        bool defaultValue)
    {
        var value =
            await _settingProvider.GetOrNullAsync(
                settingName
            );

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var result)
            ? result
            : defaultValue;
    }

    private async Task<string> GetSettingValueAsync(
        string settingName,
        string defaultValue)
    {
        var value =
            await _settingProvider.GetOrNullAsync(
                settingName
            );

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();
    }

    private sealed record ProductVariantInfo(
        Guid ProductVariantId,
        string ProductName,
        string Color,
        string Size,
        int StockQuantity,
        decimal Price,
        bool IsActive,
        bool ProductIsActive,
        bool CategoryIsActive
    );
}