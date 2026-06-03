using System;

namespace StoreManagement.Products;

public class ProductVariantSummaryDto
{
    public Guid Id { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int StockQuantity { get; set; }
}