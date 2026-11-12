using System;

namespace StoreManagement.Reports;

public class CategoryPerformanceItemDto
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public int UnitsSold { get; set; }

    /// <summary>
    /// Number of distinct orders containing at least one item from this category.
    /// </summary>
    public int OrderCount { get; set; }

    public bool IsCategoryDeleted { get; set; }
}
