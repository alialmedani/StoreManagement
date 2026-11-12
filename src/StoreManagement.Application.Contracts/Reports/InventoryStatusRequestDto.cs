using System;

namespace StoreManagement.Reports;

public class InventoryStatusRequestDto
{
    /// <summary>
    /// Optional category filter.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// When true, only variants at or below <see cref="LowStockThreshold"/>
    /// (including out-of-stock variants) are returned.
    /// </summary>
    public bool LowStockOnly { get; set; }

    /// <summary>
    /// Stock level at or below which a variant is flagged as low stock.
    /// </summary>
    public int LowStockThreshold { get; set; } = 5;
}
