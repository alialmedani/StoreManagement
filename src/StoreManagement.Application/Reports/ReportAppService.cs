using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Common;
using StoreManagement.Inventory;
using StoreManagement.Orders;
using StoreManagement.Permissions;
using StoreManagement.Products;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace StoreManagement.Reports;

[Authorize]
public class ReportAppService :
    ApplicationService,
    IReportAppService
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<ProductVariant, Guid> _productVariantRepository;
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

    public ReportAppService(
        IRepository<Order, Guid> orderRepository,
        IRepository<ProductVariant, Guid> productVariantRepository,
        IRepository<StockMovement, Guid> stockMovementRepository,
        IDataFilter<ISoftDelete> softDeleteFilter)
    {
        _orderRepository = orderRepository;
        _productVariantRepository = productVariantRepository;
        _stockMovementRepository = stockMovementRepository;
        _softDeleteFilter = softDeleteFilter;
    }

    [Authorize(StoreManagementPermissions.Reports.SalesSummary)]
    public async Task<SalesSummaryDto> GetSalesSummaryAsync(
        SalesSummaryRequestDto input)
    {
        var fromDate = input.FromDate.Date;
        var toDateExclusive = input.ToDate.Date.AddDays(1);

        var query =
            await _orderRepository.GetQueryableAsync();

        // Order-level filter. An explicit Status overrides the default
        // behaviour of counting only orders that represent a real sale.
        query = ApplyOrderStatusFilter(query, input.Status);

        if (input.PaymentStatus.HasValue)
        {
            query = query.Where(order =>
                order.PaymentStatus ==
                input.PaymentStatus.Value
            );
        }

        // Revenue side: Order.TotalAmount bucketed by CreationTime.
        var orderRows =
            await AsyncExecuter.ToListAsync(
                query
                    .Where(order =>
                        order.CreationTime >= fromDate &&
                        order.CreationTime < toDateExclusive
                    )
                    .Select(order => new OrderRow
                    {
                        CreationTime = order.CreationTime,
                        TotalAmount = order.TotalAmount,
                        Status = order.Status
                    })
            );

        // Collected side: OrderPayment.Amount bucketed by PaymentDate.
        // Same order-level filter, but its own date range on PaymentDate.
        var paymentRows =
            await AsyncExecuter.ToListAsync(
                query
                    .SelectMany(order => order.Payments)
                    .Where(payment =>
                        payment.PaymentDate >= fromDate &&
                        payment.PaymentDate < toDateExclusive
                    )
                    .Select(payment => new PaymentRow
                    {
                        PaymentDate = payment.PaymentDate,
                        Amount = payment.Amount,
                        PaymentMethod = payment.PaymentMethod
                    })
            );

        var buckets = orderRows
            .GroupBy(row =>
                GetPeriodStart(row.CreationTime, input.GroupBy))
            .OrderBy(group => group.Key)
            .Select(group => new SalesSummaryBucketDto
            {
                PeriodStart = group.Key,
                PeriodEnd =
                    GetPeriodEnd(group.Key, input.GroupBy),
                Revenue = group.Sum(row => row.TotalAmount),
                OrderCount = group.Count()
            })
            .ToList();

        var byStatus = orderRows
            .GroupBy(row => row.Status)
            .OrderBy(group => group.Key)
            .Select(group => new SalesSummaryStatusBreakdownDto
            {
                Status = group.Key,
                OrderCount = group.Count(),
                Revenue = group.Sum(row => row.TotalAmount)
            })
            .ToList();

        var byPaymentMethod = paymentRows
            .GroupBy(row => row.PaymentMethod)
            .OrderBy(group => group.Key)
            .Select(group =>
                new SalesSummaryPaymentMethodBreakdownDto
                {
                    PaymentMethod = group.Key,
                    Amount = group.Sum(row => row.Amount),
                    PaymentCount = group.Count()
                })
            .ToList();

        return new SalesSummaryDto
        {
            TotalRevenue = orderRows.Sum(row => row.TotalAmount),
            TotalCollected = paymentRows.Sum(row => row.Amount),
            TotalOrders = orderRows.Count,
            Buckets = buckets,
            ByStatus = byStatus,
            ByPaymentMethod = byPaymentMethod
        };
    }

    [Authorize(StoreManagementPermissions.Reports.InventoryStatus)]
    public async Task<InventoryStatusDto> GetInventoryStatusAsync(
        InventoryStatusRequestDto input)
    {
        var threshold = input.LowStockThreshold < 0
            ? 0
            : input.LowStockThreshold;

        var query = await _productVariantRepository.GetQueryableAsync();

        if (input.CategoryId.HasValue &&
            input.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(variant =>
                variant.Product.CategoryId == input.CategoryId.Value);
        }

        if (input.LowStockOnly)
        {
            query = query.Where(variant =>
                variant.StockQuantity <= threshold);
        }

        var rows = await AsyncExecuter.ToListAsync(
            query
                .OrderBy(variant => variant.Product.Category.Name)
                .ThenBy(variant => variant.Product.Name)
                .ThenBy(variant => variant.Color)
                .ThenBy(variant => variant.Size)
                .Select(variant => new InventoryStatusItemDto
                {
                    ProductVariantId = variant.Id,
                    ProductId = variant.ProductId,
                    ProductName = variant.Product.Name,
                    CategoryId = variant.Product.CategoryId,
                    CategoryName = variant.Product.Category.Name,
                    Color = variant.Color,
                    Size = variant.Size,
                    StockQuantity = variant.StockQuantity,
                    Price = variant.Product.Price,
                    CostPrice = variant.CostPrice
                })
        );

        foreach (var row in rows)
        {
            row.StockValue = row.StockQuantity * row.Price;
            row.IsOutOfStock = row.StockQuantity <= 0;
            row.IsLowStock =
                row.StockQuantity > 0 &&
                row.StockQuantity <= threshold;
        }

        return new InventoryStatusDto
        {
            Items = rows,
            TotalStockValue = rows.Sum(row => row.StockValue),
            TotalUnits = rows.Sum(row => row.StockQuantity),
            VariantCount = rows.Count,
            OutOfStockCount = rows.Count(row => row.IsOutOfStock),
            LowStockCount = rows.Count(row => row.IsLowStock)
        };
    }

    [Authorize(StoreManagementPermissions.Reports.TopSellers)]
    public async Task<TopSellersDto> GetTopSellersAsync(
        TopSellersRequestDto input)
    {
        var itemRows = await GetSoldItemRowsAsync(
            input.FromDate,
            input.ToDate,
            input.Status,
            paymentStatus: null,
            categoryId: null,
            productId: null,
            input.IncludeDeleted
        );

        IEnumerable<TopSellerItemDto> ranked;

        if (input.GroupByVariant)
        {
            ranked = itemRows
                .GroupBy(row => row.ProductVariantId)
                .Select(group =>
                {
                    var first = group.First();

                    return new TopSellerItemDto
                    {
                        ProductId = first.ProductId,
                        ProductVariantId = group.Key,
                        Name = first.ProductName,
                        Color = first.Color,
                        Size = first.Size,
                        QuantitySold = group.Sum(row => row.Quantity),
                        Revenue = group.Sum(row => row.LineTotal),
                        IsProductDeleted = first.IsProductDeleted,
                        IsVariantDeleted = first.IsVariantDeleted
                    };
                });
        }
        else
        {
            ranked = itemRows
                .GroupBy(row => row.ProductId)
                .Select(group =>
                {
                    var first = group.First();

                    return new TopSellerItemDto
                    {
                        ProductId = group.Key,
                        ProductVariantId = null,
                        Name = first.ProductName,
                        Color = null,
                        Size = null,
                        QuantitySold = group.Sum(row => row.Quantity),
                        Revenue = group.Sum(row => row.LineTotal),
                        IsProductDeleted = first.IsProductDeleted,
                        IsVariantDeleted = false
                    };
                });
        }

        ranked = input.Direction == TopSellersDirection.Bottom
            ? ranked
                .OrderBy(item => item.QuantitySold)
                .ThenBy(item => item.Revenue)
            : ranked
                .OrderByDescending(item => item.QuantitySold)
                .ThenByDescending(item => item.Revenue);

        return new TopSellersDto
        {
            Items = ranked
                .Take(input.Top)
                .ToList()
        };
    }

    [Authorize(StoreManagementPermissions.Reports.StockMovements)]
    public async Task<PagedResultDto<ReportStockMovementDto>>
        GetStockMovementsAsync(ReportStockMovementRequestDto input)
    {
        using (_softDeleteFilter.Disable())
        {
            var query = await _stockMovementRepository.GetQueryableAsync();

            query = query.Where(movement => !movement.IsDeleted);

            if (input.ProductVariantId.HasValue &&
                input.ProductVariantId.Value != Guid.Empty)
            {
                query = query.Where(movement =>
                    movement.ProductVariantId ==
                    input.ProductVariantId.Value);
            }

            if (input.MovementType.HasValue)
            {
                query = query.Where(movement =>
                    movement.MovementType == input.MovementType.Value);
            }

            if (input.FromDate.HasValue)
            {
                var fromDate = input.FromDate.Value.Date;

                query = query.Where(movement =>
                    movement.CreationTime >= fromDate);
            }

            if (input.ToDate.HasValue)
            {
                var toDateExclusive =
                    input.ToDate.Value.Date.AddDays(1);

                query = query.Where(movement =>
                    movement.CreationTime < toDateExclusive);
            }

            if (!input.IncludeDeleted)
            {
                query = query.Where(movement =>
                    !movement.ProductVariant.IsDeleted &&
                    !movement.ProductVariant.Product.IsDeleted);
            }

            if (DevExtremeDataSourceHelper.HasLoadOptions(input))
            {
                var dxRowQuery = query.Select(MapToStockMovementRowExpression());

                var dxRows = await DevExtremeDataSourceHelper.LoadAsync(dxRowQuery, input);

                var dxItems = dxRows.Items
                    .Select(MapRowToDto)
                    .ToList();

                return new PagedResultDto<ReportStockMovementDto>(
                    dxRows.TotalCount,
                    dxItems
                );
            }

            query = string.IsNullOrWhiteSpace(input.Sorting)
                ? query.OrderByDescending(movement => movement.CreationTime)
                : ApplyStockMovementSorting(query, input.Sorting);

            var totalCount = await AsyncExecuter.CountAsync(query);

            var rows = await AsyncExecuter.ToListAsync(
                query
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .Select(MapToStockMovementRowExpression())
            );

            var items = rows
                .Select(MapRowToDto)
                .ToList();

            return new PagedResultDto<ReportStockMovementDto>(
                totalCount,
                items
            );
        }
    }

    private static Expression<Func<StockMovement, StockMovementRow>> MapToStockMovementRowExpression()
    {
        return movement => new StockMovementRow
        {
            Id = movement.Id,
            Date = movement.CreationTime,
            ProductVariantId = movement.ProductVariantId,
            ProductName = movement.ProductVariant.Product.Name,
            Color = movement.ProductVariant.Color,
            Size = movement.ProductVariant.Size,
            OldQuantity = movement.OldQuantity,
            Change = movement.QuantityChange,
            NewQuantity = movement.NewQuantity,
            MovementType = movement.MovementType,
            SourceType = movement.SourceType,
            ReferenceId = movement.ReferenceId,
            Note = movement.Note,
            IsProductDeleted = movement.ProductVariant.Product.IsDeleted,
            IsVariantDeleted = movement.ProductVariant.IsDeleted
        };
    }

    private static ReportStockMovementDto MapRowToDto(StockMovementRow row)
    {
        return new ReportStockMovementDto
        {
            Id = row.Id,
            Date = row.Date,
            ProductVariantId = row.ProductVariantId,
            ProductVariantName = BuildVariantName(
                row.ProductName,
                row.Color,
                row.Size),
            OldQuantity = row.OldQuantity,
            Change = row.Change,
            NewQuantity = row.NewQuantity,
            Reason = new LookupDto
            {
                Id = (int)row.MovementType,
                Name = row.MovementType.ToString()
            },
            SourceType = new LookupDto
            {
                Id = (int)row.SourceType,
                Name = row.SourceType.ToString()
            },
            ReferenceId = row.ReferenceId,
            Note = row.Note,
            IsProductDeleted = row.IsProductDeleted,
            IsVariantDeleted = row.IsVariantDeleted
        };
    }

    [Authorize(StoreManagementPermissions.Reports.CategoryPerformance)]
    public async Task<CategoryPerformanceDto> GetCategoryPerformanceAsync(
        CategoryPerformanceRequestDto input)
    {
        var itemRows = await GetSoldItemRowsAsync(
            input.FromDate,
            input.ToDate,
            input.Status,
            input.PaymentStatus,
            categoryId: null,
            productId: null,
            input.IncludeDeleted
        );

        var items = itemRows
            .GroupBy(row => row.CategoryId)
            .Select(group => new CategoryPerformanceItemDto
            {
                CategoryId = group.Key,
                CategoryName = group.First().CategoryName,
                Revenue = group.Sum(row => row.LineTotal),
                UnitsSold = group.Sum(row => row.Quantity),
                OrderCount = group
                    .Select(row => row.OrderId)
                    .Distinct()
                    .Count(),
                IsCategoryDeleted = group.First().IsCategoryDeleted
            })
            .OrderByDescending(item => item.Revenue)
            .ToList();

        return new CategoryPerformanceDto
        {
            Items = items,
            TotalRevenue = items.Sum(item => item.Revenue),
            TotalUnitsSold = items.Sum(item => item.UnitsSold)
        };
    }

    [Authorize(StoreManagementPermissions.Reports.ProfitMargin)]
    public async Task<ProfitMarginDto> GetProfitMarginAsync(
        ProfitMarginRequestDto input)
    {
        var itemRows = await GetSoldItemRowsAsync(
            input.FromDate,
            input.ToDate,
            input.Status,
            paymentStatus: null,
            input.CategoryId,
            input.ProductId,
            input.IncludeDeleted
        );

        List<ProfitMarginItemDto> items;

        if (input.GroupByVariant)
        {
            items = itemRows
                .GroupBy(row => row.ProductVariantId)
                .Select(group => BuildProfitMarginItem(
                    group.First().ProductId,
                    group.Key,
                    group.First().ProductName,
                    group.First().Color,
                    group.First().Size,
                    group))
                .ToList();
        }
        else
        {
            items = itemRows
                .GroupBy(row => row.ProductId)
                .Select(group => BuildProfitMarginItem(
                    group.Key,
                    null,
                    group.First().ProductName,
                    null,
                    null,
                    group))
                .ToList();
        }

        items = items
            .OrderByDescending(item => item.GrossProfit)
            .ToList();

        var totalRevenue = items.Sum(item => item.Revenue);
        var totalCogs = items.Sum(item => item.CostOfGoodsSold);
        var totalGrossProfit = totalRevenue - totalCogs;

        var hasCompleteCurrentCogs = items.All(item =>
            item.CurrentCostOfGoodsSold.HasValue);

        return new ProfitMarginDto
        {
            Items = items,
            TotalRevenue = totalRevenue,
            TotalCostOfGoodsSold = totalCogs,
            TotalGrossProfit = totalGrossProfit,
            TotalMarginPercent = totalRevenue > 0
                ? decimal.Round(
                    totalGrossProfit / totalRevenue * 100m, 2)
                : 0m,
            HasCompleteCostData = items.All(item => item.HasCostData),
            TotalCurrentCostOfGoodsSold = hasCompleteCurrentCogs
                ? items.Sum(item => item.CurrentCostOfGoodsSold!.Value)
                : null,
            HasAnyCostChangedSinceSale =
                items.Any(item => item.HasCostChangedSinceSale)
        };
    }

    private static ProfitMarginItemDto BuildProfitMarginItem(
        Guid productId,
        Guid? productVariantId,
        string name,
        string? color,
        string? size,
        IEnumerable<SoldItemRow> rows)
    {
        var rowList = rows.ToList();
        var first = rowList.First();

        var quantitySold = rowList.Sum(row => row.Quantity);
        var revenue = rowList.Sum(row => row.LineTotal);

        // Historical: a unit has cost data only when its per-sale cost
        // snapshot is present.
        var hasCostData = rowList.All(row =>
            row.CostPriceAtSale.HasValue);

        var cogs = rowList.Sum(row =>
            row.Quantity * (row.CostPriceAtSale ?? 0m));

        var grossProfit = revenue - cogs;

        var hasCompleteCurrentCost = rowList.All(row =>
            row.CurrentCostPrice.HasValue);

        var currentCogs = hasCompleteCurrentCost
            ? rowList.Sum(row => row.Quantity * row.CurrentCostPrice!.Value)
            : (decimal?)null;

        var hasCostChangedSinceSale = rowList.Any(row =>
            row.CostPriceAtSale.HasValue &&
            row.CurrentCostPrice.HasValue &&
            row.CostPriceAtSale.Value != row.CurrentCostPrice.Value);

        return new ProfitMarginItemDto
        {
            ProductId = productId,
            ProductVariantId = productVariantId,
            Name = name,
            Color = color,
            Size = size,
            QuantitySold = quantitySold,
            Revenue = revenue,
            CostOfGoodsSold = cogs,
            GrossProfit = grossProfit,
            MarginPercent = revenue > 0
                ? decimal.Round(grossProfit / revenue * 100m, 2)
                : 0m,
            HasCostData = hasCostData,
            IsProductDeleted = first.IsProductDeleted,
            IsVariantDeleted = productVariantId.HasValue && first.IsVariantDeleted,
            CurrentCostOfGoodsSold = currentCogs,
            HasCostChangedSinceSale = hasCostChangedSinceSale
        };
    }

    /// <summary>
    /// Projects order items inside the date range to an in-memory list,
    /// applying the same order-status convention as the sales-summary
    /// report (an explicit Status overrides the default; see
    /// <see cref="ApplyOrderStatusFilter"/>).
    /// </summary>
    private async Task<List<SoldItemRow>> GetSoldItemRowsAsync(
        DateTime fromDateInput,
        DateTime toDateInput,
        OrderStatus? status,
        OrderPaymentStatus? paymentStatus,
        Guid? categoryId,
        Guid? productId,
        bool includeDeleted)
    {
        using (_softDeleteFilter.Disable())
        {
            var fromDate = fromDateInput.Date;
            var toDateExclusive = toDateInput.Date.AddDays(1);

            var query = await _orderRepository.GetQueryableAsync();

            query = query.Where(order => !order.IsDeleted);

            query = ApplyOrderStatusFilter(query, status);

            if (paymentStatus.HasValue)
            {
                query = query.Where(order =>
                    order.PaymentStatus == paymentStatus.Value);
            }

            query = query.Where(order =>
                order.CreationTime >= fromDate &&
                order.CreationTime < toDateExclusive);

            var itemsQuery = query.SelectMany(order => order.Items);

            itemsQuery = itemsQuery.Where(item => !item.IsDeleted);

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                itemsQuery = itemsQuery.Where(item =>
                    item.ProductVariant.Product.CategoryId ==
                    categoryId.Value);
            }

            if (productId.HasValue && productId.Value != Guid.Empty)
            {
                itemsQuery = itemsQuery.Where(item =>
                    item.ProductVariant.ProductId == productId.Value);
            }

            if (!includeDeleted)
            {
                itemsQuery = itemsQuery.Where(item =>
                    !item.ProductVariant.IsDeleted &&
                    !item.ProductVariant.Product.IsDeleted &&
                    !item.ProductVariant.Product.Category.IsDeleted);
            }

            return await AsyncExecuter.ToListAsync(
                itemsQuery.Select(item => new SoldItemRow
                {
                    OrderId = item.OrderId,
                    ProductId = item.ProductVariant.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    CategoryId = item.ProductVariant.Product.CategoryId,
                    CategoryName = item.ProductVariant.Product.Category.Name,
                    Color = item.Color,
                    Size = item.Size,
                    Quantity = item.Quantity,
                    LineTotal = item.LineTotal,
                    CostPriceAtSale = item.CostPriceAtSale,
                    CurrentCostPrice = item.ProductVariant.CostPrice,
                    IsProductDeleted = item.ProductVariant.Product.IsDeleted,
                    IsVariantDeleted = item.ProductVariant.IsDeleted,
                    IsCategoryDeleted = item.ProductVariant.Product.Category.IsDeleted
                })
            );
        }
    }

    private static string BuildVariantName(
        string productName,
        string color,
        string size)
    {
        return $"{productName} ({color} / {size})";
    }

    /// <summary>
    /// Single source of truth for which orders count as a real sale in
    /// reports. An explicit <paramref name="explicitStatus"/> (e.g. asking
    /// for Cancelled or Draft specifically) always wins. Otherwise only
    /// <see cref="OrderStatus.Confirmed"/> orders are included - Draft
    /// orders never had stock deducted or a cost snapshot taken, and
    /// Cancelled orders didn't result in a sale either.
    /// </summary>
    private static IQueryable<Order> ApplyOrderStatusFilter(
        IQueryable<Order> query,
        OrderStatus? explicitStatus)
    {
        return explicitStatus.HasValue
            ? query.Where(order => order.Status == explicitStatus.Value)
            : query.Where(order => order.Status == OrderStatus.Confirmed);
    }

    private static IQueryable<StockMovement> ApplyStockMovementSorting(
        IQueryable<StockMovement> query,
        string sorting)
    {
        return sorting.Trim().ToLowerInvariant() switch
        {
            "date" or "date asc" or "creationtime asc" =>
                query.OrderBy(movement => movement.CreationTime),

            "date desc" or "creationtime desc" =>
                query.OrderByDescending(movement => movement.CreationTime),

            "change" or "change asc" or "quantitychange asc" =>
                query.OrderBy(movement => movement.QuantityChange),

            "change desc" or "quantitychange desc" =>
                query.OrderByDescending(movement => movement.QuantityChange),

            _ => query.OrderByDescending(movement => movement.CreationTime)
        };
    }

    private static DateTime GetPeriodStart(
        DateTime value,
        SalesSummaryGroupBy groupBy)
    {
        var date = value.Date;

        return groupBy switch
        {
            // Week starts on Monday.
            SalesSummaryGroupBy.Week =>
                date.AddDays(
                    -(((int)date.DayOfWeek + 6) % 7)
                ),

            SalesSummaryGroupBy.Month =>
                new DateTime(date.Year, date.Month, 1),

            _ => date
        };
    }

    private static DateTime GetPeriodEnd(
        DateTime periodStart,
        SalesSummaryGroupBy groupBy)
    {
        return groupBy switch
        {
            SalesSummaryGroupBy.Week =>
                periodStart.AddDays(7),

            SalesSummaryGroupBy.Month =>
                periodStart.AddMonths(1),

            _ => periodStart.AddDays(1)
        };
    }

    private sealed class OrderRow
    {
        public DateTime CreationTime { get; set; }

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; }
    }

    private sealed class PaymentRow
    {
        public DateTime PaymentDate { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
    }

    private sealed class SoldItemRow
    {
        public Guid OrderId { get; set; }

        public Guid ProductId { get; set; }

        public Guid ProductVariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }

        public decimal? CostPriceAtSale { get; set; }

        public decimal? CurrentCostPrice { get; set; }

        public bool IsProductDeleted { get; set; }

        public bool IsVariantDeleted { get; set; }

        public bool IsCategoryDeleted { get; set; }
    }

    private sealed class StockMovementRow
    {
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public Guid ProductVariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public int OldQuantity { get; set; }

        public int Change { get; set; }

        public int NewQuantity { get; set; }

        public StockMovementType MovementType { get; set; }

        public StockMovementSourceType SourceType { get; set; }

        public Guid? ReferenceId { get; set; }

        public string? Note { get; set; }

        public bool IsProductDeleted { get; set; }

        public bool IsVariantDeleted { get; set; }
    }
}
