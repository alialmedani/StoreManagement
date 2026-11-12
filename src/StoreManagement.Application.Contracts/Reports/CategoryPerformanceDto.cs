using System.Collections.Generic;

namespace StoreManagement.Reports;

public class CategoryPerformanceDto
{
    public List<CategoryPerformanceItemDto> Items { get; set; } = new();

    public decimal TotalRevenue { get; set; }

    public int TotalUnitsSold { get; set; }
}
