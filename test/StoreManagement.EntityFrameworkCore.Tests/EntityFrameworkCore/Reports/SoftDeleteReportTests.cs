using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using StoreManagement.Reports;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Reports;

[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class SoftDeleteReportTests : ReportTestBase
{
    // ---------- TopSellers ----------

    [Fact]
    public async Task TopSellers_Includes_Deleted_Product_When_IncludeDeleted_True_With_Flag_And_Original_Name()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        var originalName = (await WithUnitOfWorkAsync(() => ProductRepository.GetAsync(catalog.ProductId))).Name;
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 2);
        await SoftDeleteProductAsync(catalog.ProductId);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetTopSellersAsync(new TopSellersRequestDto
            {
                FromDate = from,
                ToDate = to,
                Top = 10000,
                IncludeDeleted = true
            }));

        var item = report.Items.Single(i => i.ProductId == catalog.ProductId);
        item.IsProductDeleted.ShouldBeTrue();
        item.Name.ShouldBe(originalName);
    }

    [Fact]
    public async Task TopSellers_Excludes_Deleted_Product_When_IncludeDeleted_False()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 2);
        await SoftDeleteProductAsync(catalog.ProductId);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetTopSellersAsync(new TopSellersRequestDto
            {
                FromDate = from,
                ToDate = to,
                Top = 10000,
                IncludeDeleted = false
            }));

        report.Items.ShouldNotContain(i => i.ProductId == catalog.ProductId);
    }

    [Fact]
    public async Task TopSellers_Never_Includes_A_SoftDeleted_Order_Regardless_Of_IncludeDeleted()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 2);
        await SoftDeleteOrderAsync(orderId);

        var (from, to) = WideRange();

        foreach (var includeDeleted in new[] { true, false })
        {
            var report = await WithUnitOfWorkAsync(() =>
                ReportService.GetTopSellersAsync(new TopSellersRequestDto
                {
                    FromDate = from,
                    ToDate = to,
                    Top = 10000,
                    IncludeDeleted = includeDeleted
                }));

            report.Items.ShouldNotContain(
                i => i.ProductId == catalog.ProductId,
                $"IncludeDeleted={includeDeleted} must not resurrect a soft-deleted order");
        }
    }

    [Fact]
    public async Task TopSellers_Never_Includes_A_SoftDeleted_OrderItem_Regardless_Of_IncludeDeleted()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 2);

        // Soft-delete the single line item while the order itself stays active.
        await WithUnitOfWorkAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId);
            var item = order.Items.Single();
            await OrderItemRepository.DeleteAsync(item, autoSave: true);
        });

        var (from, to) = WideRange();

        foreach (var includeDeleted in new[] { true, false })
        {
            var report = await WithUnitOfWorkAsync(() =>
                ReportService.GetTopSellersAsync(new TopSellersRequestDto
                {
                    FromDate = from,
                    ToDate = to,
                    Top = 10000,
                    IncludeDeleted = includeDeleted
                }));

            report.Items.ShouldNotContain(i => i.ProductId == catalog.ProductId);
        }
    }

    // ---------- CategoryPerformance ----------

    [Fact]
    public async Task CategoryPerformance_Includes_Deleted_Category_When_IncludeDeleted_True()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 3);
        await SoftDeleteCategoryAsync(catalog.CategoryId);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetCategoryPerformanceAsync(new CategoryPerformanceRequestDto
            {
                FromDate = from,
                ToDate = to,
                IncludeDeleted = true
            }));

        var item = report.Items.Single(i => i.CategoryId == catalog.CategoryId);
        item.IsCategoryDeleted.ShouldBeTrue();
        item.UnitsSold.ShouldBe(3);
    }

    [Fact]
    public async Task CategoryPerformance_Excludes_Deleted_Category_When_IncludeDeleted_False()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 3);
        await SoftDeleteCategoryAsync(catalog.CategoryId);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetCategoryPerformanceAsync(new CategoryPerformanceRequestDto
            {
                FromDate = from,
                ToDate = to,
                IncludeDeleted = false
            }));

        report.Items.ShouldNotContain(i => i.CategoryId == catalog.CategoryId);
    }

    // ---------- ProfitMargin ----------

    [Fact]
    public async Task ProfitMargin_Includes_Deleted_Variant_When_IncludeDeleted_True_And_Excludes_When_False()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, price: 100m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);
        await SoftDeleteVariantAsync(catalog.VariantId);

        var (from, to) = WideRange();

        var included = await WithUnitOfWorkAsync(() =>
            ReportService.GetProfitMarginAsync(new ProfitMarginRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductId = catalog.ProductId,
                GroupByVariant = true,
                IncludeDeleted = true
            }));

        var item = included.Items.Single(i => i.ProductVariantId == catalog.VariantId);
        item.IsVariantDeleted.ShouldBeTrue();

        var excluded = await WithUnitOfWorkAsync(() =>
            ReportService.GetProfitMarginAsync(new ProfitMarginRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductId = catalog.ProductId,
                GroupByVariant = true,
                IncludeDeleted = false
            }));

        excluded.Items.ShouldNotContain(i => i.ProductVariantId == catalog.VariantId);
    }

    [Fact]
    public async Task ProfitMargin_Never_Includes_A_SoftDeleted_Order_Regardless_Of_IncludeDeleted()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, price: 100m);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);
        await SoftDeleteOrderAsync(orderId);

        var (from, to) = WideRange();

        foreach (var includeDeleted in new[] { true, false })
        {
            var report = await WithUnitOfWorkAsync(() =>
                ReportService.GetProfitMarginAsync(new ProfitMarginRequestDto
                {
                    FromDate = from,
                    ToDate = to,
                    ProductId = catalog.ProductId,
                    GroupByVariant = true,
                    IncludeDeleted = includeDeleted
                }));

            report.Items.ShouldBeEmpty();
        }
    }

    // ---------- StockMovements ----------

    [Fact]
    public async Task StockMovements_Includes_Deleted_Variant_When_IncludeDeleted_True_And_Excludes_When_False()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);
        await SoftDeleteVariantAsync(catalog.VariantId);

        var (from, to) = WideRange();

        var included = await WithUnitOfWorkAsync(() =>
            ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductVariantId = catalog.VariantId,
                IncludeDeleted = true
            }));
        included.Items.ShouldNotBeEmpty();
        included.Items.ShouldAllBe(m => m.IsVariantDeleted);

        var excluded = await WithUnitOfWorkAsync(() =>
            ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductVariantId = catalog.VariantId,
                IncludeDeleted = false
            }));
        excluded.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task StockMovements_Never_Includes_A_SoftDeleted_Movement_Regardless_Of_IncludeDeleted()
    {
        var catalog = await CreateCatalogAsync(cost: 10m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        // Soft-delete every movement for this variant.
        await WithUnitOfWorkAsync(async () =>
        {
            var movements = (await StockMovementRepository.GetListAsync())
                .Where(m => m.ProductVariantId == catalog.VariantId)
                .ToList();

            foreach (var movement in movements)
            {
                await StockMovementRepository.DeleteAsync(movement, autoSave: true);
            }
        });

        var (from, to) = WideRange();

        foreach (var includeDeleted in new[] { true, false })
        {
            var report = await WithUnitOfWorkAsync(() =>
                ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
                {
                    FromDate = from,
                    ToDate = to,
                    ProductVariantId = catalog.VariantId,
                    IncludeDeleted = includeDeleted
                }));

            report.Items.ShouldBeEmpty();
            report.TotalCount.ShouldBe(0);
        }
    }

    // ---------- InventoryStatus (always excludes deleted; no override param) ----------

    [Fact]
    public async Task InventoryStatus_Always_Excludes_SoftDeleted_Variant()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, color: "Blue", size: "L");

        // A second, live variant in the same category/product acts as a positive control.
        var liveVariantId = await WithUnitOfWorkAsync(async () =>
        {
            var variant = new Products.ProductVariant(
                Guid.NewGuid(), catalog.ProductId, "Green", "S", 7, true, 10m);
            await VariantRepository.InsertAsync(variant, autoSave: true);
            return variant.Id;
        });

        await SoftDeleteVariantAsync(catalog.VariantId);

        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetInventoryStatusAsync(new InventoryStatusRequestDto
            {
                CategoryId = catalog.CategoryId
            }));

        report.Items.ShouldContain(i => i.ProductVariantId == liveVariantId);
        report.Items.ShouldNotContain(i => i.ProductVariantId == catalog.VariantId);
    }

    // ---------- SalesSummary regression (never joins catalog) ----------

    [Fact]
    public async Task SalesSummary_Output_Is_Identical_Whether_Or_Not_Catalog_Is_SoftDeleted()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, price: 100m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        var (from, to) = WideRange();
        var request = new SalesSummaryRequestDto { FromDate = from, ToDate = to };

        var before = await WithUnitOfWorkAsync(() => ReportService.GetSalesSummaryAsync(request));

        await SoftDeleteVariantAsync(catalog.VariantId);
        await SoftDeleteProductAsync(catalog.ProductId);
        await SoftDeleteCategoryAsync(catalog.CategoryId);

        var after = await WithUnitOfWorkAsync(() => ReportService.GetSalesSummaryAsync(request));

        after.TotalRevenue.ShouldBe(before.TotalRevenue);
        after.TotalCollected.ShouldBe(before.TotalCollected);
        after.TotalOrders.ShouldBe(before.TotalOrders);
        after.Buckets.Count.ShouldBe(before.Buckets.Count);
        after.ByStatus.Count.ShouldBe(before.ByStatus.Count);
    }
}
