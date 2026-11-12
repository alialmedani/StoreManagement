using System;

namespace StoreManagement.Reports;

public class InventoryStatusItemDto
{
    public Guid ProductVariantId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    /// <summary>
    /// Product selling price (variants do not carry their own price).
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// StockQuantity multiplied by Price.
    /// </summary>
    public decimal StockValue { get; set; }

    public decimal? CostPrice { get; set; }

    public bool IsOutOfStock { get; set; }

    public bool IsLowStock { get; set; }
}
