using System;
using StoreManagement.Orders;

namespace StoreManagement.Reports;

public class TopSellersRequestDto
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    /// <summary>
    /// True ranks per variant; false aggregates across each product's variants.
    /// </summary>
    public bool GroupByVariant { get; set; }

    /// <summary>
    /// Number of rows to return.
    /// </summary>
    public int Top { get; set; } = 10;

    public TopSellersDirection Direction { get; set; } = TopSellersDirection.Top;

    /// <summary>
    /// Optional order status filter. When omitted, cancelled orders are excluded.
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// Unlike ABP's usual default-false soft-delete behaviour, this defaults to TRUE
    /// so historical totals stay stable even after a product/variant is soft-deleted.
    /// Set to false to exclude rows tied to soft-deleted catalog records.
    /// </summary>
    public bool IncludeDeleted { get; set; } = true;
}
