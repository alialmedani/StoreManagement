using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using StoreManagement.Reports;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Reports;

[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class ReportQueryBehaviorTests : ReportTestBase
{
    // ---------- StockMovements: pagination / sorting / date range ----------

    [Fact]
    public async Task StockMovements_Paginates_And_Reports_Total_Count()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, stock: 100);

        // Five Sale movements (quantity changes -1..-5) for a single variant.
        for (var qty = 1; qty <= 5; qty++)
        {
            await CreateConfirmedOrderAsync(catalog.VariantId, quantity: qty);
        }

        var (from, to) = WideRange();
        var page = await WithUnitOfWorkAsync(() =>
            ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductVariantId = catalog.VariantId,
                SkipCount = 0,
                MaxResultCount = 2
            }));

        page.TotalCount.ShouldBe(5);
        page.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task StockMovements_Sorting_By_Change_Ascending_And_Descending()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, stock: 100);

        for (var qty = 1; qty <= 3; qty++)
        {
            await CreateConfirmedOrderAsync(catalog.VariantId, quantity: qty);
        }

        var (from, to) = WideRange();

        var asc = await WithUnitOfWorkAsync(() =>
            ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductVariantId = catalog.VariantId,
                Sorting = "change asc",
                MaxResultCount = 100
            }));
        // Changes are -1, -2, -3 -> ascending puts the most negative first.
        asc.Items.First().Change.ShouldBe(-3);

        var desc = await WithUnitOfWorkAsync(() =>
            ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductVariantId = catalog.VariantId,
                Sorting = "change desc",
                MaxResultCount = 100
            }));
        desc.Items.First().Change.ShouldBe(-1);
    }

    [Fact]
    public async Task StockMovements_Excludes_Movements_Outside_The_Date_Range()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, stock: 100);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        var (pastFrom, pastTo) = PastRange();
        var past = await WithUnitOfWorkAsync(() =>
            ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
            {
                FromDate = pastFrom,
                ToDate = pastTo,
                ProductVariantId = catalog.VariantId,
                MaxResultCount = 100
            }));
        past.TotalCount.ShouldBe(0);

        var (from, to) = WideRange();
        var current = await WithUnitOfWorkAsync(() =>
            ReportService.GetStockMovementsAsync(new ReportStockMovementRequestDto
            {
                FromDate = from,
                ToDate = to,
                ProductVariantId = catalog.VariantId,
                MaxResultCount = 100
            }));
        current.TotalCount.ShouldBe(1);
    }

    // ---------- SalesSummary: Day / Week / Month grouping math ----------

    [Fact]
    public async Task SalesSummary_Day_Grouping_Buckets_Span_Single_Days()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, price: 100m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetSalesSummaryAsync(new SalesSummaryRequestDto
            {
                FromDate = from,
                ToDate = to,
                GroupBy = SalesSummaryGroupBy.Day
            }));

        report.Buckets.ShouldNotBeEmpty();
        report.Buckets.ShouldAllBe(b => b.PeriodEnd == b.PeriodStart.AddDays(1));
    }

    [Fact]
    public async Task SalesSummary_Week_Grouping_Buckets_Start_On_Monday_And_Span_Seven_Days()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, price: 100m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetSalesSummaryAsync(new SalesSummaryRequestDto
            {
                FromDate = from,
                ToDate = to,
                GroupBy = SalesSummaryGroupBy.Week
            }));

        report.Buckets.ShouldNotBeEmpty();
        report.Buckets.ShouldAllBe(b => b.PeriodStart.DayOfWeek == DayOfWeek.Monday);
        report.Buckets.ShouldAllBe(b => b.PeriodEnd == b.PeriodStart.AddDays(7));
    }

    [Fact]
    public async Task SalesSummary_Month_Grouping_Buckets_Start_On_First_Of_Month()
    {
        var catalog = await CreateCatalogAsync(cost: 10m, price: 100m);
        await CreateConfirmedOrderAsync(catalog.VariantId, quantity: 1);

        var (from, to) = WideRange();
        var report = await WithUnitOfWorkAsync(() =>
            ReportService.GetSalesSummaryAsync(new SalesSummaryRequestDto
            {
                FromDate = from,
                ToDate = to,
                GroupBy = SalesSummaryGroupBy.Month
            }));

        report.Buckets.ShouldNotBeEmpty();
        report.Buckets.ShouldAllBe(b => b.PeriodStart.Day == 1);
        report.Buckets.ShouldAllBe(b => b.PeriodEnd == b.PeriodStart.AddMonths(1));
    }
}
