using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using StoreManagement.Inventory;
using StoreManagement.Orders;
using StoreManagement.Reports;
using Volo.Abp;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Reports;

[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class OrderCostSnapshotTests : ReportTestBase
{
    // ---- Section 1: Order confirmation & cost snapshot ----

    [Fact]
    public async Task Confirming_An_Order_Captures_CostPriceAtSale_On_Each_Item()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 2);

        var snapshots = await GetItemSnapshotsAsync(orderId);

        var snapshot = snapshots.ShouldHaveSingleItem();
        snapshot.ShouldBe(10_000m);
    }

    [Fact]
    public async Task Draft_Order_Items_Have_Null_CostPriceAtSale()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m);
        var orderId = await CreateDraftOrderAsync(catalog.VariantId, quantity: 2);

        var snapshots = await GetItemSnapshotsAsync(orderId);

        snapshots.ShouldHaveSingleItem().ShouldBeNull();
    }

    [Fact]
    public async Task Confirming_A_Confirmed_Order_Again_Is_Blocked_And_Preserves_Snapshot()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        // Order.Confirm() guards on Status == Draft, so a second confirm throws
        // rather than ever re-running the snapshot logic.
        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var order = await LoadOrderAsync(orderId);
                await OrderManager.ConfirmAsync(order);
                await OrderRepository.UpdateAsync(order, autoSave: true);
            });
        });

        var snapshots = await GetItemSnapshotsAsync(orderId);
        snapshots.ShouldHaveSingleItem().ShouldBe(10_000m);
    }

    // ---- Section 2: Historical cost accuracy ----

    [Fact]
    public async Task Snapshot_Reflects_Cost_At_Confirmation_And_Is_Not_Affected_By_Later_Cost_Changes()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m, stock: 100);

        // First sale at cost 10,000.
        var firstOrderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        // Cost rises to 12,000 through the sole owner of cost changes.
        await WithUnitOfWorkAsync(async () =>
        {
            var variant = await VariantRepository.GetAsync(catalog.VariantId);
            await CostManager.ChangeCostPriceAsync(variant, 12_000m);
        });

        // Second sale after the change.
        var secondOrderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        var firstSnapshot = (await GetItemSnapshotsAsync(firstOrderId)).ShouldHaveSingleItem();
        var secondSnapshot = (await GetItemSnapshotsAsync(secondOrderId)).ShouldHaveSingleItem();

        firstSnapshot.ShouldBe(10_000m);
        secondSnapshot.ShouldBe(12_000m);
    }

    [Fact]
    public async Task ProfitMargin_Uses_Historical_Snapshot_And_Flags_Cost_Change()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m, stock: 100, price: 25_000m);

        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        await WithUnitOfWorkAsync(async () =>
        {
            var variant = await VariantRepository.GetAsync(catalog.VariantId);
            await CostManager.ChangeCostPriceAsync(variant, 12_000m);
        });

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetProfitMarginAsync(new ProfitMarginRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductId = catalog.ProductId,
                GroupByVariant = true
            }));

        var item = report.Items.ShouldHaveSingleItem();
        item.HasCostData.ShouldBeTrue();
        item.CostOfGoodsSold.ShouldBe(10_000m);               // historical snapshot, not current
        item.CurrentCostOfGoodsSold.ShouldBe(12_000m);        // informational only
        item.HasCostChangedSinceSale.ShouldBeTrue();
        item.GrossProfit.ShouldBe(25_000m - 10_000m);
    }

    [Fact]
    public async Task ProfitMargin_Reports_Null_Snapshot_As_No_Cost_Data_Without_Fabricating_Profit()
    {
        // Variant with null cost => confirmation snapshots null (legacy-like item).
        var catalog = await CreateCatalogAsync(cost: null, price: 25_000m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetProfitMarginAsync(new ProfitMarginRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductId = catalog.ProductId,
                GroupByVariant = true
            }));

        var item = report.Items.ShouldHaveSingleItem();
        item.HasCostData.ShouldBeFalse();
        item.CostOfGoodsSold.ShouldBe(0m);     // no fabricated cost
        report.HasCompleteCostData.ShouldBeFalse();
    }

    // ---- Section 4: Cancellation lifecycle (no return/reversal feature exists) ----

    [Fact]
    public async Task Cancelling_A_Draft_Order_Creates_No_Sale_Movement_And_No_Snapshot()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m, stock: 50);
        var orderId = await CreateDraftOrderAsync(catalog.VariantId, quantity: 3);

        await WithUnitOfWorkAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId);
            await OrderManager.CancelAsync(order, "changed mind");
            await OrderRepository.UpdateAsync(order, autoSave: true);
        });

        // Draft never touched stock: only the opening-stock movement (if any) exists, no Sale.
        var movements = await GetMovementsAsync(catalog.VariantId);
        movements.ShouldNotContain(m => m.MovementType == StockMovementType.Sale);

        var snapshots = await GetItemSnapshotsAsync(orderId);
        snapshots.ShouldHaveSingleItem().ShouldBeNull();
    }

    [Fact]
    public async Task Cancelling_A_Confirmed_Unpaid_Order_Reverses_Stock_Without_Cost_History()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m, stock: 50);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 4);

        var afterConfirm = await WithUnitOfWorkAsync(() => VariantRepository.GetAsync(catalog.VariantId));
        afterConfirm.StockQuantity.ShouldBe(46);

        await WithUnitOfWorkAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId);
            await OrderManager.CancelAsync(order, "store cancel");
            await OrderRepository.UpdateAsync(order, autoSave: true);
        });

        var afterCancel = await WithUnitOfWorkAsync(() => VariantRepository.GetAsync(catalog.VariantId));
        afterCancel.StockQuantity.ShouldBe(50);   // fully restored

        var movements = await GetMovementsAsync(catalog.VariantId);
        movements.ShouldContain(m => m.MovementType == StockMovementType.Sale);
        movements.ShouldContain(m => m.MovementType == StockMovementType.OrderCancellation);

        // Cancellation must not fabricate any cost-history rows.
        (await GetCostHistoryAsync(catalog.VariantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancelled_Order_Is_Excluded_From_ProfitMargin_By_Default()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m, stock: 50, price: 25_000m);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        await WithUnitOfWorkAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId);
            await OrderManager.CancelAsync(order, "store cancel");
            await OrderRepository.UpdateAsync(order, autoSave: true);
        });

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetProfitMarginAsync(new ProfitMarginRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductId = catalog.ProductId,
                GroupByVariant = true
            }));

        // Default (no explicit Status) excludes cancelled orders.
        report.Items.ShouldNotContain(i => i.ProductId == catalog.ProductId);
    }

    [Fact]
    public async Task A_Paid_Order_Cannot_Be_Cancelled()
    {
        var catalog = await CreateCatalogAsync(cost: 10_000m, stock: 50, price: 100m);
        var orderId = await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        await WithUnitOfWorkAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId);
            OrderManager.RecordPayment(order, 100m, PaymentMethod.Cash, null, null, null);
            await OrderRepository.UpdateAsync(order, autoSave: true);
        });

        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var order = await LoadOrderAsync(orderId);
                await OrderManager.CancelAsync(order, "too late");
                await OrderRepository.UpdateAsync(order, autoSave: true);
            });
        });
    }
}
