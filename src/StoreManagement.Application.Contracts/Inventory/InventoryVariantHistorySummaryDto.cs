using System;

namespace StoreManagement.Inventory;

public class InventoryVariantHistorySummaryDto
{
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int CurrentStockQuantity { get; set; }

    public int TotalMovements { get; set; }

    public int TotalInboundQuantity { get; set; }

    public int TotalOutboundQuantity { get; set; }

    public DateTime? LastMovementTime { get; set; }
}