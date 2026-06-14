 using System;
using System.Linq;
using System.Threading.Tasks;
using StoreManagement.Inventory;
using StoreManagement.Products;
using StoreManagement.Settings;
using Volo.Abp;
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
            customerPhone,
            note
        );
    }

    public async Task AddItemAsync(
        Order order,
        Guid productVariantId,
        int quantity,
        decimal unitPrice)
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

        /*
         * The order may already contain the same variant and price.
         * Validate the final requested quantity, not only the new
         * quantity being added.
         */
        var existingQuantity = order.Items
            .Where(item =>
                item.ProductVariantId == productVariantId &&
                item.UnitPrice == unitPrice)
            .Sum(item => item.Quantity);

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
            unitPrice
        );
    }

    public async Task ConfirmAsync(Order order)
    {
        var allowNegativeStock =
            await IsSettingEnabledAsync(
                StoreManagementSettings.AllowNegativeStock,
                defaultValue: false
            );

        foreach (var item in order.Items)
        {
            var variantInfo =
                await GetAvailableProductVariantInfoAsync(
                    item.ProductVariantId
                );

            if (!allowNegativeStock &&
                variantInfo.StockQuantity < item.Quantity)
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

    public async Task CancelAsync(Order order)
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

        /*
         * Order.Cancel() also rejects cancellation when
         * PaidAmount is greater than zero.
         */
        order.Cancel();

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
        bool IsActive,
        bool ProductIsActive,
        bool CategoryIsActive
    );
}
 
