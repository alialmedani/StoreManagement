using System;
using StoreManagement.Common;

namespace StoreManagement.Categories;

public class CategoryStockSummaryDto
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int ProductCount { get; set; }

    public int ActiveProducts { get; set; }

    public int InactiveProducts { get; set; }

    public int VariantCount { get; set; }

    public int ActiveVariants { get; set; }

    public int InactiveVariants { get; set; }

    public int TotalStockQuantity { get; set; }

    public int LowStockThreshold { get; set; }

    public LookupDto AvailabilityStatus { get; set; } = new();

    public int OutOfStockProducts { get; set; }

    public int LowStockProducts { get; set; }

    public int InStockProducts { get; set; }

    public int OutOfStockVariants { get; set; }

    public int LowStockVariants { get; set; }

    public int InStockVariants { get; set; }
}