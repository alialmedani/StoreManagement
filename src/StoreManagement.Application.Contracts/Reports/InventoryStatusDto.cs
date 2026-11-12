using System.Collections.Generic;

namespace StoreManagement.Reports;

public class InventoryStatusDto
{
    public List<InventoryStatusItemDto> Items { get; set; } = new();

    public decimal TotalStockValue { get; set; }

    public int TotalUnits { get; set; }

    public int VariantCount { get; set; }

    public int OutOfStockCount { get; set; }

    public int LowStockCount { get; set; }
}
