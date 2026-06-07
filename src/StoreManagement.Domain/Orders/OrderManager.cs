using System;
using System.Linq;
using System.Threading.Tasks;
using StoreManagement.Inventory;
using StoreManagement.Products;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Linq;

namespace StoreManagement.Orders;

public class OrderManager : DomainService
{
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly InventoryManager _inventoryManager;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public OrderManager(
        IRepository<ProductVariant, Guid> productVariantRepository,
        InventoryManager inventoryManager,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _productVariantRepository = productVariantRepository;
        _inventoryManager = inventoryManager;
        _asyncExecuter = asyncExecuter;
    }

    public Task<Order> CreateAsync(
        string customerName,
        string? customerPhone,
        string? note)
    {
        var order = new Order(
            GuidGenerator.Create(),
            GenerateOrderNumber(),
            customerName,
            customerPhone,
            note
        );

        return Task.FromResult(order);
    }

    public async Task AddItemAsync(
        Order order,
        Guid productVariantId,
        int quantity,
        decimal unitPrice)
    {
        var variantInfo = await GetAvailableProductVariantInfoAsync(productVariantId);

        if (variantInfo.StockQuantity < quantity)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderInsufficientStock);
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
        foreach (var item in order.Items)
        {
            var variantInfo = await GetAvailableProductVariantInfoAsync(item.ProductVariantId);

            if (variantInfo.StockQuantity < item.Quantity)
            {
                throw new BusinessException(StoreManagementDomainErrorCodes.OrderInsufficientStock);
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

    public async Task CancelAsync(Order order)
    {
        var wasConfirmed = order.IsConfirmed();

        order.Cancel();

        if (!wasConfirmed)
        {
            return;
        }

        foreach (var item in order.Items)
        {
            await _inventoryManager.RecordOrderCancellationAsync(
                order.Id,
                item.ProductVariantId,
                item.Quantity,
                $"Cancellation of order {order.OrderNumber}"
            );
        }
    }

    private async Task<ProductVariantInfo> GetAvailableProductVariantInfoAsync(Guid productVariantId)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderProductVariantNotFound);
        }

        var query = await _productVariantRepository.GetQueryableAsync();

        var variantInfo = await _asyncExecuter.FirstOrDefaultAsync(
            query
                .Where(variant => variant.Id == productVariantId)
                .Select(variant => new ProductVariantInfo(
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
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderProductVariantNotFound);
        }

        if (!variantInfo.IsActive || !variantInfo.ProductIsActive || !variantInfo.CategoryIsActive)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderProductVariantInactive);
        }

        return variantInfo;
    }

    private string GenerateOrderNumber()
    {
        var suffix = GuidGenerator.Create()
            .ToString("N")
            .Substring(0, 6)
            .ToUpperInvariant();

        return $"ORD-{Clock.Now:yyyyMMddHHmmssfff}-{suffix}";
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