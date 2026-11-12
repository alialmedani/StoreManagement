using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StoreManagement.Categories;
using StoreManagement.Inventory;
using StoreManagement.Orders;
using StoreManagement.Products;
using StoreManagement.Reports;
using Volo.Abp.Domain.Repositories;

namespace StoreManagement.EntityFrameworkCore.Reports;

/// <summary>
/// Shared fixture helpers for the cost-tracking / soft-delete report integration tests.
/// Every derived test class gets its own in-memory SQLite database (one per test class),
/// so helpers create uniquely-named catalog rows and assertions target the specific
/// product/variant/category/order they created rather than global aggregates.
/// </summary>
public abstract class ReportTestBase : StoreManagementEntityFrameworkCoreTestBase
{
    protected readonly IRepository<Category, Guid> CategoryRepository;
    protected readonly IRepository<Product, Guid> ProductRepository;
    protected readonly IRepository<ProductVariant, Guid> VariantRepository;
    protected readonly IRepository<ProductVariantCostHistory, Guid> CostHistoryRepository;
    protected readonly IRepository<Order, Guid> OrderRepository;
    protected readonly IRepository<OrderItem, Guid> OrderItemRepository;
    protected readonly IRepository<StockMovement, Guid> StockMovementRepository;
    protected readonly OrderManager OrderManager;
    protected readonly ProductVariantCostManager CostManager;
    protected readonly ReportAppService ReportService;

    protected ReportTestBase()
    {
        CategoryRepository = GetRequiredService<IRepository<Category, Guid>>();
        ProductRepository = GetRequiredService<IRepository<Product, Guid>>();
        VariantRepository = GetRequiredService<IRepository<ProductVariant, Guid>>();
        CostHistoryRepository = GetRequiredService<IRepository<ProductVariantCostHistory, Guid>>();
        OrderRepository = GetRequiredService<IRepository<Order, Guid>>();
        OrderItemRepository = GetRequiredService<IRepository<OrderItem, Guid>>();
        StockMovementRepository = GetRequiredService<IRepository<StockMovement, Guid>>();
        OrderManager = GetRequiredService<OrderManager>();
        CostManager = GetRequiredService<ProductVariantCostManager>();
        ReportService = GetRequiredService<ReportAppService>();
    }

    /// <summary>Wide date range that safely brackets "now" regardless of local/UTC skew.</summary>
    protected static (DateTime From, DateTime To) WideRange()
    {
        return (DateTime.Now.AddDays(-3), DateTime.Now.AddDays(3));
    }

    /// <summary>A range entirely in the past, so nothing created "now" falls inside it.</summary>
    protected static (DateTime From, DateTime To) PastRange()
    {
        return (DateTime.Now.AddYears(-2), DateTime.Now.AddYears(-2).AddDays(1));
    }

    protected sealed record Catalog(Guid CategoryId, Guid ProductId, Guid VariantId);

    protected async Task<Catalog> CreateCatalogAsync(
        decimal? cost = null,
        int stock = 100,
        decimal price = 100m,
        string size = "M",
        string color = "Red")
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var category = new Category(
                Guid.NewGuid(),
                "Cat-" + Guid.NewGuid().ToString("N"),
                null,
                CategorySizeType.Clothing);
            await CategoryRepository.InsertAsync(category, autoSave: true);

            var product = new Product(
                Guid.NewGuid(),
                "Prod-" + Guid.NewGuid().ToString("N"),
                null,
                price,
                category.Id);
            await ProductRepository.InsertAsync(product, autoSave: true);

            var variant = new ProductVariant(
                Guid.NewGuid(),
                product.Id,
                color,
                size,
                stock,
                true,
                cost);
            await VariantRepository.InsertAsync(variant, autoSave: true);

            return new Catalog(category.Id, product.Id, variant.Id);
        });
    }

    /// <summary>Creates a Draft order with a single line (never confirmed).</summary>
    protected async Task<Guid> CreateDraftOrderAsync(Guid variantId, int quantity)
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var order = await OrderManager.CreateAsync(
                "Customer", "Address", "0100000000", null);

            await OrderManager.AddItemAsync(order, variantId, quantity);

            await OrderRepository.InsertAsync(order, autoSave: true);

            return order.Id;
        });
    }

    /// <summary>Creates and confirms an order with a single line.</summary>
    protected async Task<Guid> CreateConfirmedOrderAsync(Guid variantId, int quantity)
    {
        var orderId = await CreateDraftOrderAsync(variantId, quantity);

        await WithUnitOfWorkAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId);
            await OrderManager.ConfirmAsync(order);
            await OrderRepository.UpdateAsync(order, autoSave: true);
        });

        return orderId;
    }

    protected async Task<Order> LoadOrderAsync(Guid orderId)
    {
        var query = await OrderRepository.WithDetailsAsync(
            order => order.Items,
            order => order.Payments);

        return query.Single(order => order.Id == orderId);
    }

    protected async Task<List<decimal?>> GetItemSnapshotsAsync(Guid orderId)
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId);
            return order.Items.Select(item => item.CostPriceAtSale).ToList();
        });
    }

    protected async Task<List<ProductVariantCostHistory>> GetCostHistoryAsync(Guid variantId)
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var all = await CostHistoryRepository.GetListAsync();
            return all.Where(h => h.ProductVariantId == variantId).ToList();
        });
    }

    protected async Task<List<StockMovement>> GetMovementsAsync(Guid variantId)
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var all = await StockMovementRepository.GetListAsync();
            return all.Where(m => m.ProductVariantId == variantId).ToList();
        });
    }

    protected Task SoftDeleteProductAsync(Guid productId)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var product = await ProductRepository.GetAsync(productId);
            await ProductRepository.DeleteAsync(product, autoSave: true);
        });
    }

    protected Task SoftDeleteVariantAsync(Guid variantId)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var variant = await VariantRepository.GetAsync(variantId);
            await VariantRepository.DeleteAsync(variant, autoSave: true);
        });
    }

    protected Task SoftDeleteCategoryAsync(Guid categoryId)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var category = await CategoryRepository.GetAsync(categoryId);
            await CategoryRepository.DeleteAsync(category, autoSave: true);
        });
    }

    protected Task SoftDeleteOrderAsync(Guid orderId)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var order = await OrderRepository.GetAsync(orderId);
            await OrderRepository.DeleteAsync(order, autoSave: true);
        });
    }
}
